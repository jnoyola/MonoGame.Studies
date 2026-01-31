using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Study1.ContentFramework.Math;
using Study1.ContentFramework.Models;

namespace Study1.Game;

public class AnimationPlayer
{
    private const int MaxBoneCount = 72;
    private const float DefaultTransitionDuration = 0.15f;
    private const float DefaultFadeDuration = 0.15f;
    private static readonly Vector3 Vector3One = Vector3.One;
    private static readonly Quaternion QuaternionIdentity = Quaternion.Identity;

    private readonly Transform[] ScratchBoneTransforms = new Transform[MaxBoneCount];
    private readonly Transform[] ScratchAdditiveAccumulationTransforms = new Transform[MaxBoneCount];
    private readonly BitArray HasScratchAdditiveAccumulationTranslation = new(MaxBoneCount);
    private readonly BitArray HasScratchAdditiveAccumulationRotation = new(MaxBoneCount);
    private readonly BitArray HasScratchAdditiveAccumulationScale = new(MaxBoneCount);

    private struct AnimationChannelState
    {
        public ushort TranslationFrameIndex { get; set; }
        public ushort RotationFrameIndex { get; set; }
        public ushort ScaleFrameIndex { get; set; }
    }

    private struct AnimationChannelSnapshot
    {
        public Vector3 Translation { get; set; }
        public Quaternion Rotation { get; set; }
        public Vector3 Scale { get; set; }
    }

    public static void Play(
        ref AnimationState state,
        ref AnimationSet animationSet,
        AnimationLayer layer,
        string animation,
        float weight = 1.0f,
        float playbackSpeed = 1.0f,
        float transitionDuration = DefaultTransitionDuration)
    {
        if (animationSet.AnimationLayerDefinitions.IsAdditiveLayer(layer))
        {
            PlayAdditive(ref state, ref animationSet, layer, animation, weight, playbackSpeed, transitionDuration);
        }
        else
        {
            PlayOverride(ref state, ref animationSet, layer, animation, weight, playbackSpeed, transitionDuration);
        }
    }

    public static void Stop(ref AnimationState state, ref AnimationSet animationSet, AnimationLayer? layer = null, float fadeDuration = DefaultFadeDuration)
    {
        if (layer.HasValue)
        {
            if (animationSet.AnimationLayerDefinitions.IsAdditiveLayer(layer.Value))
            {
                // TODO: stop additive layer
            }
            else
            {
                var layerIndex = animationSet.AnimationLayerDefinitions.GetLayerIndex(layer.Value);
                StopOverrideLayer(ref state.OverrideLayers[layerIndex], fadeDuration);
            }
        }
        else
        {
            for (int layerIndex = 0; layerIndex < AnimationLayerDefinitions.MaxOverrideLayerCount; ++layerIndex)
            {
                StopOverrideLayer(ref state.OverrideLayers[layerIndex], fadeDuration);
            }

            // TODO: stop all additive layers
        }
    }

    public static void UpdateTime(ref AnimationState state, GameTime gameTime)
    {
        ResolveAdditiveClips(ref state);

        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        for (int layerIndex = 0; layerIndex < AnimationLayerDefinitions.MaxOverrideLayerCount; ++layerIndex)
        {
            UpdateOverrideLayerTime(ref state.OverrideLayers[layerIndex], dt);
        }
        for (int layerIndex = 0; layerIndex < AnimationLayerDefinitions.MaxAdditiveLayerCount; ++layerIndex)
        {
            for (int clipIndex = 0; clipIndex < AdditiveLayerState.MaxAdditiveClipCount; ++clipIndex)
            {
                ref var clipState = ref state.AdditiveLayers[layerIndex].Clips[clipIndex];
                UpdateAdditiveClipTime(ref clipState, dt);
            }
        }
    }

    public void SampleBones(ref AnimationState state, ref AnimationSet animationSet, Span<Bone> bones, Span<Matrix> boneMatrices)
    {
        // Step 1: Sample and combine override layers.
        for (int layerIndex = 0; layerIndex < animationSet.AnimationLayerDefinitions.OverrideLayerCount; ++layerIndex)
        {
            SampleOverrideLayer(ref state, ref animationSet, layerIndex, bones);
        }

        // Step 2: Sample and apply additive layers.
        HasScratchAdditiveAccumulationTranslation.SetAll(false);
        HasScratchAdditiveAccumulationRotation.SetAll(false);
        HasScratchAdditiveAccumulationScale.SetAll(false);
        for (int layerIndex = 0; layerIndex < animationSet.AnimationLayerDefinitions.AdditiveLayerCount; ++layerIndex)
        {
            SampleAdditiveLayer(ref state, ref animationSet, layerIndex, bones);
        }
        ApplyAdditiveDeltas(bones);

        // Step 3: Convert scratch transforms to matrices.
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            ScratchBoneTransforms[boneIndex].ToMatrix(out boneMatrices[boneIndex]);
        }
    }

    private static void PlayOverride(
        ref AnimationState state,
        ref AnimationSet animationSet,
        AnimationLayer layer,
        string animation,
        float weight,
        float playbackSpeed,
        float transitionDuration)
    {
        var resolvedAnimation = animationSet.GetAnimation(animation);
        var layerIndex = animationSet.AnimationLayerDefinitions.GetLayerIndex(layer);
        ref var layerState = ref state.OverrideLayers[layerIndex];
        if (resolvedAnimation != layerState.Animation)
        {
            // It's a new animation, but we already had one playing. Transition from the previous one.
            SnapshotTransitionState(ref state, layerIndex, transitionDuration);

            // Then start the new animation.
            layerState.Animation = animationSet.GetAnimation(animation);
            layerState.Time = 0;
        }

        // Update the playback settings, and fade it in.
        layerState.PlaybackSpeed = playbackSpeed;
        if (transitionDuration == 0)
        {
            layerState.Weight = weight;
            layerState.TargetWeight = weight;
            layerState.WeightVelocity = 0;
        }
        else
        {
            layerState.TargetWeight = weight;
            layerState.WeightVelocity = (layerState.TargetWeight - layerState.Weight) / transitionDuration;
        }
    }

    private static void PlayAdditive(
        ref AnimationState state,
        ref AnimationSet animationSet,
        AnimationLayer layer,
        string animation,
        float weight,
        float playbackSpeed,
        float fadeDuration)
    {
        var layerIndex = animationSet.AnimationLayerDefinitions.GetLayerIndex(layer);
        ref var layerState = ref state.AdditiveLayers[layerIndex];
        if (layerState.RequestedClipIndex >= AdditiveLayerState.MaxAdditiveClipCount)
        {
            // We're already at max additive clip capacity for this layer for this frame.
            return;
        }

        layerState.RequestedClips[layerState.RequestedClipIndex++] = new AdditiveClipRequest
        {
            Animation = animationSet.GetAnimation(animation),
            Weight = weight,
            PlaybackSpeed = playbackSpeed,
            FadeDuration = fadeDuration
        };
    }

    private static void StopOverrideLayer(ref OverrideLayerState layerState, float fadeDuration = DefaultFadeDuration)
    {
        if (fadeDuration == 0)
        {
            layerState.Animation = null;
            layerState.TransitionAnimation = null;
            layerState.Weight = 0;
            layerState.TargetWeight = 0;
            layerState.WeightVelocity = 0;
        }
        else
        {
            // Update the weight velocity, but take the minimum (more negative) value so that we don't slow down the fade
            // in case Stop was called multiple times in a row.
            layerState.TargetWeight = 0;
            layerState.WeightVelocity = Math.Min(layerState.WeightVelocity, -layerState.Weight / fadeDuration);
        }
    }

    private static void StopAdditiveClip(ref AdditiveClipState clipState, float fadeDuration = DefaultFadeDuration)
    {
        if (fadeDuration == 0)
        {
            clipState.Animation = null;
            clipState.Weight = 0;
            clipState.TargetWeight = 0;
            clipState.WeightVelocity = 0;
        }
        else
        {
            // Update the weight velocity, but take the minimum (more negative) value so that we don't slow down the fade
            // in case Stop was called multiple times in a row.
            clipState.TargetWeight = 0;
            clipState.WeightVelocity = Math.Min(clipState.WeightVelocity, -clipState.Weight / fadeDuration);
        }
    }

    private static void ResolveAdditiveClips(ref AnimationState state)
    {
        for (int layerIndex = 0; layerIndex < AnimationLayerDefinitions.MaxAdditiveLayerCount; ++layerIndex)
        {
            ref var layerState = ref state.AdditiveLayers[layerIndex];

            // Find which desired animations are already playing, and which slots are already occupied.
            var isClipMatched = new AdditiveLayerState.ClipFlags();
            var isSlotMatched = new AdditiveLayerState.ClipFlags();
            for (int clipIndex = 0; clipIndex < layerState.RequestedClipIndex; ++clipIndex)
            {
                // Perform a small linear scan to search for the requested animation in the current slots.
                for (int slotIndex = 0; slotIndex < AdditiveLayerState.MaxAdditiveClipCount; ++slotIndex)
                {
                    if (layerState.RequestedClips[clipIndex].Animation == layerState.Clips[slotIndex].Animation)
                    {
                        isClipMatched[clipIndex] = true;
                        isSlotMatched[slotIndex] = true;
                        break;
                    }
                }
            }

            // Find each new pair of desired animation and available slot.
            for (int clipIndex = 0, slotIndex = 0; clipIndex < layerState.RequestedClipIndex && slotIndex < AdditiveLayerState.MaxAdditiveClipCount;)
            {
                if (isClipMatched[clipIndex])
                {
                    ++clipIndex;
                }
                else if (isSlotMatched[slotIndex])
                {
                    ++slotIndex;
                }
                else
                {
                    // Set the requested clip into the available slot.
                    ref var clipRequest = ref layerState.RequestedClips[clipIndex];
                    ref var clipState = ref layerState.Clips[slotIndex];
                    clipState.Animation = clipRequest.Animation;
                    clipState.Time = 0;
                    clipState.PlaybackSpeed = clipRequest.PlaybackSpeed;
                    if (clipRequest.FadeDuration == 0)
                    {
                        clipState.Weight = clipRequest.Weight;
                        clipState.TargetWeight = clipRequest.Weight;
                        clipState.WeightVelocity = 0;
                    }
                    else
                    {
                        clipState.Weight = 0;
                        clipState.TargetWeight = clipRequest.Weight;
                        clipState.WeightVelocity = clipRequest.Weight / clipRequest.FadeDuration;
                    }

                    isSlotMatched[slotIndex] = true;
                    ++clipIndex;
                    ++slotIndex;
                }
            }

            // Fade out any old animations that are no longer desired and the slot isn't being used for a higher priority animation.
            for (int slotIndex = 0; slotIndex < AdditiveLayerState.MaxAdditiveClipCount; ++slotIndex)
            {
                if (!isSlotMatched[slotIndex] && layerState.Clips[slotIndex].Animation != null)
                {
                    StopAdditiveClip(ref layerState.Clips[slotIndex]);
                }
            }

            // Reset the request state for next frame.
            layerState.RequestedClipIndex = 0;
        }
    }

    private static void UpdateOverrideLayerTime(ref OverrideLayerState layerState, float dt)
    {
        if (layerState.Animation == null && layerState.TransitionAnimation == null)
        {
            return;
        }

        // Update the layer time, as long as we're not fading out this layer's weight.
        // If the animation ends, then begin to fade it out.
        var isLayerFading = layerState.TargetWeight == 0 && layerState.WeightVelocity < 0;
        if (layerState.Animation != null && !isLayerFading)
        {
            layerState.Time += dt * layerState.PlaybackSpeed;
            if (layerState.Time >= layerState.Animation.DurationInSeconds)
            {
                switch (layerState.Animation.WrapMode)
                {
                    case WrapMode.Once:
                        layerState.Time = layerState.Animation.DurationInSeconds;
                        StopOverrideLayer(ref layerState);
                        break;
                    case WrapMode.Loop:
                        layerState.Time %= layerState.Animation.DurationInSeconds;
                        break;
                    case WrapMode.Clamp:
                        layerState.Time = layerState.Animation.DurationInSeconds;
                        break;
                }
            }
            else if (layerState.Time < 0)
            {
                switch (layerState.Animation.WrapMode)
                {
                    case WrapMode.Once:
                        layerState.Time = 0;
                        StopOverrideLayer(ref layerState);
                        break;
                    case WrapMode.Loop:
                        layerState.Time = layerState.Animation.DurationInSeconds + (layerState.Time % layerState.Animation.DurationInSeconds);
                        break;
                    case WrapMode.Clamp:
                        layerState.Time = 0;
                        break;
                }
            }
        }

        // Update the layer's weight. Only when the weight reaches zero do we clear the animation.
        if (layerState.WeightVelocity != 0)
        {
            layerState.Weight += dt * layerState.WeightVelocity;
            if ((layerState.WeightVelocity < 0 && layerState.Weight <= layerState.TargetWeight)
                || (layerState.WeightVelocity > 0 && layerState.Weight >= layerState.TargetWeight))
            {
                if (layerState.TargetWeight == 0)
                {
                    layerState.Animation = null;
                    layerState.TransitionAnimation = null;
                    layerState.Weight = 0;
                    layerState.WeightVelocity = 0;
                }
                else
                {
                    layerState.Weight = layerState.TargetWeight;
                    layerState.WeightVelocity = 0;
                }
            }
        }

        // Update the transition time if we're transitioning from a previous animation.
        if (layerState.TransitionRemainingDuration > 0)
        {
            layerState.TransitionRemainingDuration -= dt;
            if (layerState.TransitionRemainingDuration < 0)
            {
                layerState.TransitionTotalDuration = 0;
                layerState.TransitionRemainingDuration = 0;
                layerState.TransitionAnimation = null;
            }
        }
    }

    private static void UpdateAdditiveClipTime(ref AdditiveClipState clipState, float dt)
    {
        if (clipState.Animation == null)
        {
            return;
        }

        // Update the clip time, as long as we're not fading out this clip's weight.
        // If the animation ends, then begin to fade it out.
        var isLayerFading = clipState.TargetWeight == 0 && clipState.WeightVelocity < 0;
        if (!isLayerFading)
        {
            clipState.Time += dt * clipState.PlaybackSpeed;
            if (clipState.Time >= clipState.Animation.DurationInSeconds)
            {
                switch (clipState.Animation.WrapMode)
                {
                    case WrapMode.Once:
                        clipState.Time = clipState.Animation.DurationInSeconds;
                        StopAdditiveClip(ref clipState);
                        break;
                    case WrapMode.Loop:
                        clipState.Time %= clipState.Animation.DurationInSeconds;
                        break;
                    case WrapMode.Clamp:
                        clipState.Time = clipState.Animation.DurationInSeconds;
                        break;
                }
            }
            else if (clipState.Time < 0)
            {
                switch (clipState.Animation.WrapMode)
                {
                    case WrapMode.Once:
                        clipState.Time = 0;
                        StopAdditiveClip(ref clipState);
                        break;
                    case WrapMode.Loop:
                        clipState.Time = clipState.Animation.DurationInSeconds + (clipState.Time % clipState.Animation.DurationInSeconds);
                        break;
                    case WrapMode.Clamp:
                        clipState.Time = 0;
                        break;
                }
            }
        }

        // Update the clip's weight. Only when the weight reaches zero do we clear the animation.
        if (clipState.WeightVelocity != 0)
        {
            clipState.Weight += dt * clipState.WeightVelocity;
            if ((clipState.WeightVelocity < 0 && clipState.Weight <= clipState.TargetWeight)
                || (clipState.WeightVelocity > 0 && clipState.Weight >= clipState.TargetWeight))
            {
                if (clipState.TargetWeight == 0)
                {
                    clipState.Animation = null;
                    clipState.Weight = 0;
                    clipState.WeightVelocity = 0;
                }
                else
                {
                    clipState.Weight = clipState.TargetWeight;
                    clipState.WeightVelocity = 0;
                }
            }
        }
    }

    private void SampleOverrideLayer(
        ref AnimationState state,
        ref AnimationSet animationSet,
        int layerIndex,
        Span<Bone> bones)
    {
        ref OverrideLayerDefinition layerDef = ref animationSet.AnimationLayerDefinitions.GetOverrideLayer(layerIndex);
        ref OverrideLayerState layerState = ref state.OverrideLayers[layerIndex];

        if ((layerState.Animation == null && layerState.TransitionAnimation == null)
            || layerState.Weight == 0)
        {
            if (layerIndex == 0)
            {
                for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
                {
                    ScratchBoneTransforms[boneIndex] = bones[boneIndex].LocalTransform;
                }
            }
            return;
        }

        var prevTranslationIndex = 0;
        var prevRotationIndex = 0;
        var prevScaleIndex = 0;
        var currTranslationIndex = 0;
        var currRotationIndex = 0;
        var currScaleIndex = 0;
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            if (layerDef.BoneMask != null && !layerDef.BoneMask[boneIndex])
            {
                if (layerIndex == 0)
                {
                    ScratchBoneTransforms[boneIndex] = bones[boneIndex].LocalTransform;
                }
                continue;
            }

            // Sample the previous animation if the bone is active in each channel.
            var prevTransform = bones[boneIndex].LocalTransform;
            if (layerState.TransitionAnimation != null
                && TryGetAnimationBoneIndex(layerState.TransitionAnimation, boneIndex, out var prevBoneIndex))
            {
                SampleBoneTranslation(
                    layerState.TransitionAnimation,
                    ref prevTranslationIndex,
                    prevBoneIndex,
                    layerState.TransitionTime,
                    layerState.TransitionPlaybackSpeed,
                    ref prevTransform.Translation
                );
                SampleBoneRotation(
                    layerState.TransitionAnimation,
                    ref prevRotationIndex,
                    prevBoneIndex,
                    layerState.TransitionTime,
                    layerState.TransitionPlaybackSpeed,
                    ref prevTransform.Rotation
                );
                SampleBoneScale(
                    layerState.TransitionAnimation,
                    ref prevScaleIndex,
                    prevBoneIndex,
                    layerState.TransitionTime,
                    layerState.TransitionPlaybackSpeed,
                    ref prevTransform.Scale
                );
            }
            
            // Sample the current animation if the bone is active in each channel.
            var currTransform = bones[boneIndex].LocalTransform;
            if (layerState.Animation != null
                && TryGetAnimationBoneIndex(layerState.Animation, boneIndex, out var currBoneIndex))
            {
                SampleBoneTranslation(
                    layerState.Animation,
                    ref currTranslationIndex,
                    currBoneIndex,
                    layerState.Time,
                    layerState.PlaybackSpeed,
                    ref currTransform.Translation
                );
                SampleBoneRotation(
                    layerState.Animation,
                    ref currRotationIndex,
                    currBoneIndex,
                    layerState.Time,
                    layerState.PlaybackSpeed,
                    ref currTransform.Rotation
                );
                SampleBoneScale(
                    layerState.Animation,
                    ref currScaleIndex,
                    currBoneIndex,
                    layerState.Time,
                    layerState.PlaybackSpeed,
                    ref currTransform.Scale
                );
            }
            
            // Blend the previous and current animations for this layer.
            ref Transform layerTransform = ref currTransform;
            if (layerState.TransitionAnimation == null)
            {
                // Leave the transform pointing to the current animation's transform.
            }
            else if (layerState.Animation == null)
            {
                layerTransform = prevTransform;
            }
            else
            {
                var progress = 1 - (layerState.TransitionRemainingDuration / layerState.TransitionTotalDuration);
                Interpolate(in prevTransform, in currTransform, progress, out layerTransform);
            }

            // Accumulate this layer's transform into the total.            
            if (layerIndex == 0)
            {
                ScratchBoneTransforms[boneIndex] = layerTransform;
            }
            else
            {
                Interpolate(in ScratchBoneTransforms[boneIndex], in layerTransform, state.OverrideLayers[layerIndex].Weight, out ScratchBoneTransforms[boneIndex]);
            }
        }
    }

    private void SampleAdditiveLayer(
        ref AnimationState state,
        ref AnimationSet animationSet,
        int layerIndex,
        Span<Bone> bones)
    {
        ref AdditiveLayerDefinition layerDef = ref animationSet.AnimationLayerDefinitions.GetAdditiveLayer(layerIndex);
        ref AdditiveLayerState layerState = ref state.AdditiveLayers[layerIndex];
        
        for (int clipIndex = 0; clipIndex < AdditiveLayerState.MaxAdditiveClipCount; ++clipIndex)
        {
            ref AdditiveClipState clipState = ref layerState.Clips[clipIndex];
            if (clipState.Animation == null || clipState.Weight == 0)
            {
                continue;
            }

            var translationIndex = 0;
            var rotationIndex = 0;
            var scaleIndex = 0;
            var tempTransform = new Transform();
            for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
            {
                if ((layerDef.BoneMask != null && !layerDef.BoneMask[boneIndex])
                    || !TryGetAnimationBoneIndex(clipState.Animation, boneIndex, out var animBoneIndex))
                {
                    continue;
                }

                // TODO: something seems off about the thumbs?
                ref var boneLocalTransform = ref bones[boneIndex].LocalTransform;
                if (SampleBoneTranslation(
                    clipState.Animation,
                    ref translationIndex,
                    animBoneIndex,
                    clipState.Time,
                    clipState.PlaybackSpeed,
                    ref tempTransform.Translation))
                {
                    ComputeAdditiveTranslationDelta(ref tempTransform.Translation, ref boneLocalTransform.Translation, clipState.Weight, out var delta);
                    if (HasScratchAdditiveAccumulationTranslation[boneIndex])
                    {
                        AccumulateAdditiveTranslation(ref ScratchAdditiveAccumulationTransforms[boneIndex], ref delta);
                    }
                    else
                    {
                        ScratchAdditiveAccumulationTransforms[boneIndex].Translation = delta;
                        HasScratchAdditiveAccumulationTranslation[boneIndex] = true;
                    }
                }
                if (SampleBoneRotation(
                    clipState.Animation,
                    ref rotationIndex,
                    animBoneIndex,
                    clipState.Time,
                    clipState.PlaybackSpeed,
                    ref tempTransform.Rotation))
                {
                    ComputeAdditiveRotationDelta(ref tempTransform.Rotation, ref boneLocalTransform.Rotation, clipState.Weight, out var delta);
                    if (HasScratchAdditiveAccumulationRotation[boneIndex])
                    {
                        AccumulateAdditiveRotation(ref ScratchAdditiveAccumulationTransforms[boneIndex], ref delta);
                    }
                    else
                    {
                        ScratchAdditiveAccumulationTransforms[boneIndex].Rotation = delta;
                        HasScratchAdditiveAccumulationRotation[boneIndex] = true;
                    }
                }
                if (SampleBoneScale(
                    clipState.Animation,
                    ref scaleIndex,
                    animBoneIndex,
                    clipState.Time,
                    clipState.PlaybackSpeed,
                    ref tempTransform.Scale))
                {
                    ComputeAdditiveScaleDelta(ref tempTransform.Scale, ref boneLocalTransform.Scale, clipState.Weight, out var delta);
                    if (HasScratchAdditiveAccumulationScale[boneIndex])
                    {
                        AccumulateAdditiveScale(ref ScratchAdditiveAccumulationTransforms[boneIndex], ref delta);
                    }
                    else
                    {
                        ScratchAdditiveAccumulationTransforms[boneIndex].Scale = delta;
                        HasScratchAdditiveAccumulationScale[boneIndex] = true;
                    }
                }
            }
        }
    }

    private void ApplyAdditiveDeltas(Span<Bone> bones)
    {
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            if (HasScratchAdditiveAccumulationTranslation[boneIndex])
            {
                ApplyAdditiveTranslation(ref ScratchBoneTransforms[boneIndex], ref ScratchAdditiveAccumulationTransforms[boneIndex]);
            }
            if (HasScratchAdditiveAccumulationRotation[boneIndex])
            {
                ApplyAdditiveRotation(ref ScratchBoneTransforms[boneIndex], ref ScratchAdditiveAccumulationTransforms[boneIndex]);
            }
            if (HasScratchAdditiveAccumulationScale[boneIndex])
            {
                ApplyAdditiveScale(ref ScratchBoneTransforms[boneIndex], ref ScratchAdditiveAccumulationTransforms[boneIndex]);
            }
        }
    }

    private static bool SampleBoneTranslation(
        Animation? animation,
        ref int lastChannelIndex,
        int animationBoneIndex,
        float time,
        float playbackSpeed,
        ref Vector3 result)
    {
        while (lastChannelIndex < animation?.TranslationChannels?.Length)
        {
            if (animation.TranslationChannels?[lastChannelIndex].BoneIndex < animationBoneIndex)
            {
                ++lastChannelIndex;
            }
            else
            {
                if (animation.TranslationChannels?[lastChannelIndex].BoneIndex == animationBoneIndex)
                {
                    ref var channel = ref animation.TranslationChannels[lastChannelIndex];
                    var frameIndex = FindFrameIndex(channel.Keyframes, time, playbackSpeed);
                    SampleChannel(
                        channel.Keyframes,
                        animation.DurationInSeconds,
                        frameIndex,
                        time,
                        playbackSpeed,
                        out result
                    );
                    return true;
                }
                return false;
            }
        }
        return false;
    }

    private static bool SampleBoneRotation(
        Animation? animation,
        ref int lastChannelIndex,
        int animationBoneIndex,
        float time,
        float playbackSpeed,
        ref Quaternion result)
    {
        while (lastChannelIndex < animation?.RotationChannels?.Length)
        {
            if (animation.RotationChannels?[lastChannelIndex].BoneIndex < animationBoneIndex)
            {
                ++lastChannelIndex;
            }
            else
            {
                if (animation.RotationChannels?[lastChannelIndex].BoneIndex == animationBoneIndex)
                {
                    ref var channel = ref animation.RotationChannels[lastChannelIndex];
                    var frameIndex = FindFrameIndex(channel.Keyframes, time, playbackSpeed);
                    SampleChannel(
                        channel.Keyframes,
                        animation.DurationInSeconds,
                        frameIndex,
                        time,
                        playbackSpeed,
                        out result
                    );
                    return true;
                }
                return false;
            }
        }
        return false;
    }

    private static bool SampleBoneScale(
        Animation? animation,
        ref int lastChannelIndex,
        int animationBoneIndex,
        float time,
        float playbackSpeed,
        ref Vector3 result)
    {
        while (lastChannelIndex < animation?.ScaleChannels?.Length)
        {
            if (animation.ScaleChannels?[lastChannelIndex].BoneIndex < animationBoneIndex)
            {
                ++lastChannelIndex;
            }
            else
            {
                if (animation.ScaleChannels?[lastChannelIndex].BoneIndex == animationBoneIndex)
                {
                    ref var channel = ref animation.ScaleChannels[lastChannelIndex];
                    var frameIndex = FindFrameIndex(channel.Keyframes, time, playbackSpeed);
                    SampleChannel(
                        channel.Keyframes,
                        animation.DurationInSeconds,
                        frameIndex,
                        time,
                        playbackSpeed,
                        out result
                    );
                    return true;
                }
                return false;
            }
        }
        return false;
    }

    private static bool TryGetAnimationBoneIndex([NotNullWhen(true)] Animation? animation, int animationSetBoneIndex, out int animationBoneIndex)
    {
        if (animation == null)
        {
            animationBoneIndex = 0;
            return false;
        }

        animationBoneIndex = animation.BoneIndexMapping[animationSetBoneIndex];
        return animationBoneIndex != -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ComputeAdditiveTranslationDelta(ref Vector3 additive, ref Vector3 reference, float weight, out Vector3 delta)
    {
        Vector3.Subtract(ref additive, ref reference, out delta);
        Vector3.Multiply(ref delta, weight, out delta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ComputeAdditiveRotationDelta(ref Quaternion additive, ref Quaternion reference, float weight, out Quaternion delta)
    {
        Quaternion.Inverse(ref reference, out delta);
        Quaternion.Multiply(ref delta, ref additive, out delta);
        Interpolate(in QuaternionIdentity, in delta, weight, out delta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ComputeAdditiveScaleDelta(ref Vector3 additive, ref Vector3 reference, float weight, out Vector3 delta)
    {
        Vector3.Divide(ref additive, ref reference, out delta);
        Interpolate(in Vector3One, in delta, weight, out delta);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AccumulateAdditiveTranslation(ref Transform transform, ref Vector3 delta)
    {
        Vector3.Add(ref transform.Translation, ref delta, out transform.Translation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AccumulateAdditiveRotation(ref Transform transform, ref Quaternion delta)
    {
        Quaternion.Multiply(ref transform.Rotation, ref delta, out transform.Rotation);
        // Quaternion.Normalize(ref transform.Rotation, out transform.Rotation); TODO: can this be removed?
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void AccumulateAdditiveScale(ref Transform transform, ref Vector3 delta)
    {
        Vector3.Multiply(ref transform.Scale, ref delta, out transform.Scale);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyAdditiveTranslation(ref Transform transform, ref Transform delta)
    {
        Vector3.Add(ref transform.Translation, ref delta.Translation, out transform.Translation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyAdditiveRotation(ref Transform transform, ref Transform delta)
    {
        Quaternion.Normalize(ref delta.Rotation, out delta.Rotation); // TODO: is this ok to keep?
        Quaternion.Multiply(ref transform.Rotation, ref delta.Rotation, out transform.Rotation);
        Quaternion.Normalize(ref transform.Rotation, out transform.Rotation);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ApplyAdditiveScale(ref Transform transform, ref Transform delta)
    {
        Vector3.Multiply(ref transform.Scale, ref delta.Scale, out transform.Scale);
    }

    private static ushort FindFrameIndex<T>(
        Keyframe<T>[] keyframes, float targetTime, float playbackSpeed
    ) where T : struct
    {
        int left = 0;
        int right = keyframes.Length - 1;

        if (playbackSpeed >= 0)
        {
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                int next = mid + 1;
                float midTime = keyframes[mid].Time;

                if (
                    midTime <= targetTime
                    && (next >= keyframes.Length || keyframes[next].Time > targetTime)
                )
                {
                    return (ushort)mid;
                }
                else if (midTime < targetTime)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            return (ushort)(right >= 0 ? right : 0);
        }
        else
        {
            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                int prev = mid - 1;
                float midTime = keyframes[mid].Time;

                if (
                    midTime >= targetTime
                    && (prev < 0 || keyframes[prev].Time < targetTime)
                )
                {
                    return (ushort)mid;
                }
                else if (midTime > targetTime)
                {
                    left = mid + 1;
                }
                else
                {
                    right = mid - 1;
                }
            }

            return (ushort)(right >= 0 ? right : 0);
        }
    }

    private static void SampleChannel(Keyframe<Vector3>[] keyframes, float totalDuration, ushort frameIndex, float currentTime, float playbackSpeed, out Vector3 result)
    {
        ref readonly var currKeyframe = ref keyframes[frameIndex];

        if (totalDuration > 0)
        {
            if (playbackSpeed > 0)
            {
                var nextFrameIndex = (ushort)(frameIndex + 1 < keyframes.Length ? frameIndex + 1 : 0);
                ref readonly var nextKeyframe = ref keyframes[nextFrameIndex];
                var progress =
                    MeasureTimeDistance(totalDuration, currKeyframe.Time, currentTime)
                    / MeasureTimeDistance(totalDuration, currKeyframe.Time, nextKeyframe.Time);
                Interpolate(in currKeyframe.Value, in nextKeyframe.Value, progress, out result);
                return;
            }
            else if (playbackSpeed < 0)
            {
                var nextFrameIndex = (ushort)(frameIndex - 1 >= 0 ? frameIndex - 1 : keyframes.Length - 1);
                ref readonly var nextKeyframe = ref keyframes[nextFrameIndex];
                var progress =
                    MeasureTimeDistance(totalDuration, currentTime, currKeyframe.Time)
                    / MeasureTimeDistance(totalDuration, nextKeyframe.Time, currKeyframe.Time);
                Interpolate(in currKeyframe.Value, in nextKeyframe.Value, progress, out result);
                return;
            }
        }

        result = currKeyframe.Value;
    }

    private static void SampleChannel(Keyframe<Quaternion>[] keyframes, float totalDuration, ushort frameIndex, float currentTime, float playbackSpeed, out Quaternion result)
    {
        ref readonly var currKeyframe = ref keyframes[frameIndex];

        if (totalDuration > 0)
        {
            if (playbackSpeed > 0)
            {
                var nextFrameIndex = (ushort)(frameIndex + 1 < keyframes.Length ? frameIndex + 1 : 0);
                ref readonly var nextKeyframe = ref keyframes[nextFrameIndex];
                var progress =
                    MeasureTimeDistance(totalDuration, currKeyframe.Time, currentTime)
                    / MeasureTimeDistance(totalDuration, currKeyframe.Time, nextKeyframe.Time);
                Interpolate(in currKeyframe.Value, in nextKeyframe.Value, progress, out result);
                return;
            }
            else if (playbackSpeed < 0)
            {
                var nextFrameIndex = (ushort)(frameIndex - 1 >= 0 ? frameIndex - 1 : keyframes.Length - 1);
                ref readonly var nextKeyframe = ref keyframes[nextFrameIndex];
                var progress =
                    MeasureTimeDistance(totalDuration, currentTime, currKeyframe.Time)
                    / MeasureTimeDistance(totalDuration, nextKeyframe.Time, currKeyframe.Time);
                Interpolate(in currKeyframe.Value, in nextKeyframe.Value, progress, out result);
                return;
            }
        }

        result = currKeyframe.Value;
    }

    private static void Interpolate(in Transform curr, in Transform next, float progress, out Transform result)
    {
        Interpolate(in curr.Translation, in next.Translation, progress, out result.Translation);
        Interpolate(in curr.Rotation, in next.Rotation, progress, out result.Rotation);
        Interpolate(in curr.Scale, in next.Scale, progress, out result.Scale);
    }

    private static void Interpolate(in Vector3 curr, in Vector3 next, float progress, out Vector3 result)
    {
        result = curr + (next - curr) * progress;
    }

    private static void Interpolate(in Quaternion curr, in Quaternion next, float progress, out Quaternion result)
    {
        float invProgress = 1f - progress;
        if (curr.X * next.X + curr.Y * next.Y + curr.Z * next.Z + curr.W * next.W >= 0f)
        {
            result.X = invProgress * curr.X + progress * next.X;
            result.Y = invProgress * curr.Y + progress * next.Y;
            result.Z = invProgress * curr.Z + progress * next.Z;
            result.W = invProgress * curr.W + progress * next.W;
        }
        else
        {
            result.X = invProgress * curr.X - progress * next.X;
            result.Y = invProgress * curr.Y - progress * next.Y;
            result.Z = invProgress * curr.Z - progress * next.Z;
            result.W = invProgress * curr.W - progress * next.W;
        }

        float scalar = 1f / MathF.Sqrt(result.X * result.X + result.Y * result.Y + result.Z * result.Z + result.W * result.W);
        result.X *= scalar;
        result.Y *= scalar;
        result.Z *= scalar;
        result.W *= scalar;
    }

    private static float MeasureTimeDistance(float totalDuration, float firstTime, float secondTime)
        => firstTime <= secondTime ? secondTime - firstTime : totalDuration - firstTime + secondTime;

    private static void SnapshotTransitionState(ref AnimationState state, int layerIndex, float transitionDuration)
    {
        ref var layerState = ref state.OverrideLayers[layerIndex];
        if (transitionDuration <= 0 || layerState.Animation == null)
        {
            layerState.TransitionTotalDuration = 0;
            layerState.TransitionRemainingDuration = 0;
            layerState.TransitionAnimation = null;
            return;
        }

        layerState.TransitionTotalDuration = transitionDuration;
        layerState.TransitionRemainingDuration = transitionDuration;
        layerState.TransitionAnimation = layerState.Animation;
        layerState.TransitionTime = layerState.Time;
        layerState.TransitionPlaybackSpeed = layerState.PlaybackSpeed;
    }
}

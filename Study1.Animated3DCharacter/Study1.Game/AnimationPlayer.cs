using System.Diagnostics.CodeAnalysis;
using Microsoft.Xna.Framework;
using Study1.ContentFramework.Math;
using Study1.ContentFramework.Models;

namespace Study1.Game;

public struct AnimationPlayer(AnimationSet animationSet)
{
    private const float DefaultTransitionDuration = 0.15f;
    private const float DefaultFadeDuration = 0.15f;
    private static readonly Vector3 Vector3One = Vector3.One;
    private static readonly Quaternion QuaternionIdentity = Quaternion.Identity;

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

    private readonly AnimationSet animationSet = animationSet;
    private AnimationState state;

    public void Play(
        AnimationLayer layer,
        string animation,
        float weight = 1.0f,
        float playbackSpeed = 1.0f,
        float transitionDuration = DefaultTransitionDuration)
    {
        if (animationSet.AnimationLayerDefinitions.IsAdditiveLayer(layer))
        {
            PlayAdditive(layer, animation, weight, playbackSpeed, transitionDuration);
        }
        else
        {
            PlayOverride(layer, animation, weight, playbackSpeed, transitionDuration);
        }
    }

    public void Stop(AnimationLayer? layer = null, float fadeDuration = DefaultFadeDuration)
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

    public void UpdateTime(GameTime gameTime)
    {
        ResolveAdditiveClips();

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

    public void SampleBone(Bone bone, out Matrix result)
    {
        // Check if this bone from the model exists in the animation set.
        var isBoneInAnimationSet = animationSet.TryGetBoneIndex(bone.Name, out var boneIndex);
        if (!isBoneInAnimationSet)
        {
            bone.LocalTransform.ToMatrix(out result);
            return;
        }
        
        Transform boneTransform = new();
        Transform tempTransform = new();

        // Step 1: Sample and combine override layers.
        if (!SampleOverrideLayers(0, boneIndex, in bone.LocalTransform, ref boneTransform))
        {
            boneTransform = bone.LocalTransform;
        }
        for (int layerIndex = 1; layerIndex < animationSet.AnimationLayerDefinitions.OverrideLayerCount; ++layerIndex)
        {
            if (SampleOverrideLayers(layerIndex, boneIndex, in bone.LocalTransform, ref tempTransform))
            {
                Interpolate(in boneTransform, in tempTransform, state.OverrideLayers[layerIndex].Weight, out boneTransform);
            }
        }

        // Step 2: Sample and accumulate additive layers.
        tempTransform = Transform.Identity;
        for (int layerIndex = 0; layerIndex < animationSet.AnimationLayerDefinitions.AdditiveLayerCount; ++layerIndex)
        {
            if (SampleAdditiveLayers(layerIndex, boneIndex, ref bone.LocalTransform, ref tempTransform))
            {
                ApplyAdditive(ref boneTransform, ref tempTransform);
            }
        }

        boneTransform.ToMatrix(out result);
    }

    private void PlayOverride(
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
            SnapshotTransitionState(layerIndex, transitionDuration);

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

    private void PlayAdditive(
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

    private void StopOverrideLayer(ref OverrideLayerState layerState, float fadeDuration = DefaultFadeDuration)
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

    private void StopAdditiveClip(ref AdditiveClipState clipState, float fadeDuration = DefaultFadeDuration)
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

    private void ResolveAdditiveClips()
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

    private void UpdateOverrideLayerTime(ref OverrideLayerState layerState, float dt)
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

    private void UpdateAdditiveClipTime(ref AdditiveClipState clipState, float dt)
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

    private bool SampleOverrideLayers(int layerIndex, int boneIndex, in Transform boneLocalTransform, ref Transform outTransform)
    {
        ref OverrideLayerDefinition layerDef = ref animationSet.AnimationLayerDefinitions.GetOverrideLayer(layerIndex);
        ref OverrideLayerState layerState = ref state.OverrideLayers[layerIndex];

        if ((layerState.Animation == null && layerState.TransitionAnimation == null)
            || layerState.Weight == 0
            || (layerDef.BoneMask != null && !layerDef.BoneMask[boneIndex]))
        {
            return false;
        }

        if (layerState.TransitionAnimation == null)
        {
            bool isBoneInCurr = TryGetAnimationBoneIndex(layerState.Animation, boneIndex, out var currAnimBoneIndex);
            if (!isBoneInCurr)
            {
                return false;
            }
            
            SampleAnimation(layerState.Animation!, currAnimBoneIndex, layerState.Time, layerState.PlaybackSpeed, out outTransform);
            return true;
        }
        else
        {
            bool isBoneInPrev = TryGetAnimationBoneIndex(layerState.TransitionAnimation, boneIndex, out var prevAnimBoneIndex);
            bool isBoneInCurr = TryGetAnimationBoneIndex(layerState.Animation, boneIndex, out var currAnimBoneIndex);
            if (!isBoneInPrev && !isBoneInCurr)
            {
                return false;
            }

            Transform prevTransform, currTransform;
            if (isBoneInPrev)
            {
                SampleAnimation(layerState.TransitionAnimation!, prevAnimBoneIndex, layerState.TransitionTime, layerState.PlaybackSpeed, out prevTransform);
            }
            else
            {
                prevTransform = boneLocalTransform;
            }
            if (isBoneInCurr)
            {
                SampleAnimation(layerState.Animation!, currAnimBoneIndex, layerState.Time, layerState.PlaybackSpeed, out currTransform);
            }
            else
            {
                currTransform = boneLocalTransform;
            }

            var progress = 1 - (layerState.TransitionRemainingDuration / layerState.TransitionTotalDuration);
            Interpolate(in prevTransform, in currTransform, progress, out outTransform);
            return true;
        }
    }

    private bool SampleAdditiveLayers(int layerIndex, int boneIndex, ref Transform boneLocalTransform, ref Transform accumulatedTransform)
    {
        ref AdditiveLayerDefinition layerDef = ref animationSet.AnimationLayerDefinitions.GetAdditiveLayer(layerIndex);
        ref AdditiveLayerState layerState = ref state.AdditiveLayers[layerIndex];

        if (layerDef.BoneMask != null && !layerDef.BoneMask[boneIndex])
        {
            return false;
        }

        bool isBoneInLayer = false;
        for (int clipIndex = 0; clipIndex < AdditiveLayerState.MaxAdditiveClipCount; ++clipIndex)
        {
            ref AdditiveClipState clipState = ref layerState.Clips[clipIndex];
            if (clipState.Animation == null || clipState.Weight == 0)
            {
                continue;
            }

            var isBoneInAnim = TryGetAnimationBoneIndex(clipState.Animation, boneIndex, out var animBoneIndex);
            if (!isBoneInAnim)
            {
                continue;
            }

            SampleAnimation(clipState.Animation, animBoneIndex, clipState.Time, clipState.PlaybackSpeed, out var transform);
            ComputeAdditiveDelta(ref transform, ref boneLocalTransform, out var delta);

            AccumulateAdditive(ref accumulatedTransform, ref delta, clipState.Weight);
            isBoneInLayer = true;
        }

        return isBoneInLayer;
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

    private static void SampleAnimation(Animation animation, int animationBoneIndex, float time, float playbackSpeed, out Transform transform)
    {
        var translationFrameIndex = FindFrameIndex(
            animation.BoneChannels[animationBoneIndex].Translations.Keyframes,
            time,
            playbackSpeed
        );
        var rotationFrameIndex = FindFrameIndex(
            animation.BoneChannels[animationBoneIndex].Rotations.Keyframes,
            time,
            playbackSpeed
        );
        var scaleFrameIndex = FindFrameIndex(
            animation.BoneChannels[animationBoneIndex].Scales.Keyframes,
            time,
            playbackSpeed
        );

        SampleChannel(
            animation.BoneChannels[animationBoneIndex].Translations.Keyframes,
            animation.DurationInSeconds,
            translationFrameIndex,
            time,
            playbackSpeed,
            out transform.Translation
        );
        SampleChannel(
            animation.BoneChannels[animationBoneIndex].Rotations.Keyframes,
            animation.DurationInSeconds,
            rotationFrameIndex,
            time,
            playbackSpeed,
            out transform.Rotation
        );
        SampleChannel(
            animation.BoneChannels[animationBoneIndex].Scales.Keyframes,
            animation.DurationInSeconds,
            scaleFrameIndex,
            time,
            playbackSpeed,
            out transform.Scale
        );
    }

    private static void ComputeAdditiveDelta(ref Transform pose, ref Transform reference, out Transform delta) {
        Vector3.Subtract(ref pose.Translation, ref reference.Translation, out delta.Translation);

        Quaternion.Inverse(ref reference.Rotation, out delta.Rotation);
        Quaternion.Multiply(ref delta.Rotation, ref pose.Rotation, out delta.Rotation);

        Vector3.Divide(ref pose.Scale, ref reference.Scale, out delta.Scale);
    }

    private static void AccumulateAdditive(ref Transform transform, ref Transform delta, float weight) {
        Vector3.Multiply(ref delta.Translation, weight, out delta.Translation);
        Vector3.Add(ref transform.Translation, ref delta.Translation, out transform.Translation);

        Interpolate(in QuaternionIdentity, in delta.Rotation, weight, out delta.Rotation);
        Quaternion.Multiply(ref transform.Rotation, ref delta.Rotation, out transform.Rotation);
        Quaternion.Normalize(ref transform.Rotation, out transform.Rotation);

        Interpolate(in Vector3One, in delta.Scale, weight, out delta.Scale);
        Vector3.Multiply(ref transform.Scale, ref delta.Scale, out transform.Scale);
    }

    private static void ApplyAdditive(ref Transform transform, ref Transform delta)
    {
        Vector3.Add(ref transform.Translation, ref delta.Translation, out transform.Translation);

        Quaternion.Multiply(ref transform.Rotation, ref delta.Rotation, out transform.Rotation);
        Quaternion.Normalize(ref transform.Rotation, out transform.Rotation);

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

    private void SnapshotTransitionState(int layerIndex, float transitionDuration)
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

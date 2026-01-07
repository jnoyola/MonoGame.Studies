using Microsoft.Xna.Framework;
using Study1.ContentFramework.Models;

namespace Study1.Game;

public struct AnimationPlayer(AnimationSet animationSet)
{
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

    public void Restart(int layer, string animation, float playbackSpeed = 1.0f, bool loop = false)
    {
        state.Layers[layer].Animation = animationSet.Get(animation);
        state.Layers[layer].Time = 0;
        state.Layers[layer].PlaybackSpeed = playbackSpeed;
        state.Layers[layer].IsLooping = loop;
    }

    public void Play(int layer, string animation, float playbackSpeed = 1.0f, bool loop = false, float transitionDuration = 0.15f)
    {
        var currAnimation = state.Layers[layer].Animation;
        if (currAnimation.HasValue)
        {
            if (currAnimation.Value.Name == animation)
            {
                // It's the same animation. Update the playback settings.
                state.Layers[layer].PlaybackSpeed = playbackSpeed;
                state.Layers[layer].IsLooping = loop;
                return;
            }
            else
            {
                // It's a new animation, but we already had one playing. Transition from the previous one.
                SnapshotTransitionState(layer, transitionDuration);
            }
        }

        Restart(layer, animation, playbackSpeed, loop);
    }

    public void Stop(int layer = -1)
    {
        if (layer == -1)
        {
            for (int i = 0; i < AnimationState.LayerCount; ++i)
            {
                state.Layers[i].Animation = null;
            }
        }
        else
        {
            state.Layers[layer].Animation = null;
        }
    }

    public void UpdateTime(GameTime gameTime)
    {
        for (int layer = 0; layer < AnimationState.LayerCount; ++layer)
        {
            UpdateLayerTime(ref state.Layers[layer], gameTime);
        }
    }

    public void SetBoneTransform(Bone bone, ref Matrix outMatrix)
    {
        for (int layer = 0; layer < AnimationState.LayerCount; ++layer)
        {
            if (layer == 0 && !state.Layers[layer].Animation.HasValue)
            {
                outMatrix = bone.LocalTransform;
            }

            SetLayerBoneTransform(ref state.Layers[layer], bone, ref outMatrix);
        }
    }

    private void UpdateLayerTime(ref AnimationLayer layer, GameTime gameTime)
    {
        if (!layer.Animation.HasValue)
        {
            return;
        }

        layer.Time += (float)gameTime.ElapsedGameTime.TotalSeconds * layer.PlaybackSpeed;
        if (layer.Time >= layer.Animation.Value.DurationInSeconds)
        {
            if (layer.IsLooping)
            {
                layer.Time %= layer.Animation.Value.DurationInSeconds;
            }
            else
            {
                layer.Animation = null;
                return;
            }
        }
        else if (layer.Time < 0)
        {
            if (layer.IsLooping)
            {
                layer.Time = layer.Animation.Value.DurationInSeconds + (layer.Time % layer.Animation.Value.DurationInSeconds);
            }
            else
            {
                layer.Animation = null;
                return;
            }
        }

        // Update the transition time if we're transitioning from a previous animation.
        if (layer.TransitionRemainingDuration > 0)
        {
            layer.TransitionRemainingDuration -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (layer.TransitionRemainingDuration < 0)
            {
                layer.TransitionTotalDuration = 0;
                layer.TransitionRemainingDuration = 0;
                layer.TransitionAnimation = null;
            }
        }
    }

    private void SetLayerBoneTransform(ref AnimationLayer layer, Bone bone, ref Matrix outMatrix)
    {
        if (!layer.Animation.HasValue)
        {
            return;
        }

        // TODO: make this a predetermined mapping?
        var boneIndex = 0;
        while (layer.Animation.Value.BoneChannels[boneIndex].BoneName != bone.Name)
        {
            ++boneIndex;
        }

        // If the animation doesn't have a channel for the requested bone, set the bone transform to be the bone's
        // unanimated transform by default. Note this doesn't apply the transition, and may look choppy.
        if (boneIndex == layer.Animation.Value.BoneChannels.Count)
        {
            outMatrix = bone.LocalTransform;
            return;
        }

        var translationFrameIndex = FindFrameIndex(
            layer.Animation.Value.BoneChannels[boneIndex].Translations.Keyframes,
            layer.Time,
            layer.PlaybackSpeed
        );
        var rotationFrameIndex = FindFrameIndex(
            layer.Animation.Value.BoneChannels[boneIndex].Rotations.Keyframes,
            layer.Time,
            layer.PlaybackSpeed
        );
        var scaleFrameIndex = FindFrameIndex(
            layer.Animation.Value.BoneChannels[boneIndex].Scales.Keyframes,
            layer.Time,
            layer.PlaybackSpeed
        );

        var translation = Sample(
            layer.Animation.Value.BoneChannels[boneIndex].Translations.Keyframes,
            layer.Animation.Value.DurationInSeconds,
            translationFrameIndex,
            layer.Time,
            layer.PlaybackSpeed
        );
        var rotation = Sample(
            layer.Animation.Value.BoneChannels[boneIndex].Rotations.Keyframes,
            layer.Animation.Value.DurationInSeconds,
            rotationFrameIndex,
            layer.Time,
            layer.PlaybackSpeed
        );
        var scale = Sample(
            layer.Animation.Value.BoneChannels[boneIndex].Scales.Keyframes,
            layer.Animation.Value.DurationInSeconds,
            scaleFrameIndex,
            layer.Time,
            layer.PlaybackSpeed
        );

        // If we're transitioning from a previous animation, interpolate between the transition snapshot and the current frame.
        if (layer.TransitionRemainingDuration > 0 && layer.TransitionAnimation.HasValue)
        {
            // TODO: make this a predetermined mapping?
            var transitionBoneIndex = 0;
            while (layer.TransitionAnimation.Value.BoneChannels[transitionBoneIndex].BoneName != bone.Name)
            {
                ++transitionBoneIndex;
            }

            if (transitionBoneIndex < layer.TransitionAnimation.Value.BoneChannels.Count)
            {
                var transitionTranslationFrameIndex = FindFrameIndex(
                    layer.TransitionAnimation.Value.BoneChannels[transitionBoneIndex].Translations.Keyframes,
                    layer.TransitionTime,
                    layer.TransitionPlaybackSpeed
                );
                var transitionRotationFrameIndex = FindFrameIndex(
                    layer.TransitionAnimation.Value.BoneChannels[transitionBoneIndex].Rotations.Keyframes,
                    layer.TransitionTime,
                    layer.TransitionPlaybackSpeed
                );
                var transitionScaleFrameIndex = FindFrameIndex(
                    layer.TransitionAnimation.Value.BoneChannels[transitionBoneIndex].Scales.Keyframes,
                    layer.TransitionTime,
                    layer.TransitionPlaybackSpeed
                );

                var transitionTranslation = Sample(
                    layer.TransitionAnimation.Value.BoneChannels[transitionBoneIndex].Translations.Keyframes,
                    layer.TransitionAnimation.Value.DurationInSeconds,
                    transitionTranslationFrameIndex,
                    layer.TransitionTime,
                    layer.TransitionPlaybackSpeed
                );
                var transitionRotation = Sample(
                    layer.TransitionAnimation.Value.BoneChannels[boneIndex].Rotations.Keyframes,
                    layer.TransitionAnimation.Value.DurationInSeconds,
                    transitionRotationFrameIndex,
                    layer.TransitionTime,
                    layer.TransitionPlaybackSpeed
                );
                var transitionScale = Sample(
                    layer.TransitionAnimation.Value.BoneChannels[boneIndex].Scales.Keyframes,
                    layer.TransitionAnimation.Value.DurationInSeconds,
                    transitionScaleFrameIndex,
                    layer.TransitionTime,
                    layer.TransitionPlaybackSpeed
                );

                var transitionProgress = 1 - (layer.TransitionRemainingDuration / layer.TransitionTotalDuration);
                translation = Interpolate(transitionTranslation, translation, transitionProgress);
                rotation = Interpolate(transitionRotation, rotation, transitionProgress);
                scale = Interpolate(transitionScale, scale, transitionProgress);
            }
        }

        outMatrix = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(rotation);
        outMatrix.Translation = translation;

        // If the bone is the root bone, premultiply its base transform so that the animation is applied on top of the
        // model's intended scale, rotation, and translation. Otherwise this would reset every model to match the exact
        // scale and rotation of the animation file.
        // if (bone.ParentIndex < 0)
        // {
        //     outMatrix = bone.LocalTransform * outMatrix;
        // }
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

    private static Vector3 Sample(Keyframe<Vector3>[] keyframes, float totalDuration, ushort frameIndex, float currentTime, float playbackSpeed)
    {
        ref readonly var currKeyframe = ref keyframes[frameIndex];

        if (playbackSpeed > 0)
        {
            var nextFrameIndex = (ushort)(frameIndex + 1 < keyframes.Length ? frameIndex + 1 : 0);
            ref readonly var nextKeyframe = ref keyframes[nextFrameIndex];
            var progress =
                MeasureTimeDistance(totalDuration, currKeyframe.Time, currentTime)
                / MeasureTimeDistance(totalDuration, currKeyframe.Time, nextKeyframe.Time);
            return Interpolate(currKeyframe.Value, nextKeyframe.Value, progress);
        }
        else if (playbackSpeed < 0)
        {
            var nextFrameIndex = (ushort)(frameIndex - 1 >= 0 ? frameIndex - 1 : keyframes.Length - 1);
            ref readonly var nextKeyframe = ref keyframes[nextFrameIndex];
            var progress =
                MeasureTimeDistance(totalDuration, currentTime, currKeyframe.Time)
                / MeasureTimeDistance(totalDuration, nextKeyframe.Time, currKeyframe.Time);
            return Interpolate(currKeyframe.Value, nextKeyframe.Value, progress);
        }
        else
        {
            return currKeyframe.Value;
        }
    }

    private static Quaternion Sample(Keyframe<Quaternion>[] keyframes, float totalDuration, ushort frameIndex, float currentTime, float playbackSpeed)
    {
        ref readonly var currKeyframe = ref keyframes[frameIndex];

        if (playbackSpeed > 0)
        {
            var nextFrameIndex = (ushort)(frameIndex + 1 < keyframes.Length ? frameIndex + 1 : 0);
            ref readonly var nextKeyframe = ref keyframes[nextFrameIndex];
            var progress =
                MeasureTimeDistance(totalDuration, currKeyframe.Time, currentTime)
                / MeasureTimeDistance(totalDuration, currKeyframe.Time, nextKeyframe.Time);
            return Interpolate(currKeyframe.Value, nextKeyframe.Value, progress);
        }
        else if (playbackSpeed < 0)
        {
            var nextFrameIndex = (ushort)(frameIndex - 1 >= 0 ? frameIndex - 1 : keyframes.Length - 1);
            ref readonly var nextKeyframe = ref keyframes[nextFrameIndex];
            var progress =
                MeasureTimeDistance(totalDuration, currentTime, currKeyframe.Time)
                / MeasureTimeDistance(totalDuration, nextKeyframe.Time, currKeyframe.Time);
            return Interpolate(currKeyframe.Value, nextKeyframe.Value, progress);
        }
        else
        {
            return currKeyframe.Value;
        }
    }

    private static Vector3 Interpolate(Vector3 curr, Vector3 next, float progress)
        => curr + (next - curr) * progress;

    private static Quaternion Interpolate(Quaternion curr, Quaternion next, float progress)
        => Quaternion.Lerp(curr, next, progress);

    private static float MeasureTimeDistance(float totalDuration, float firstTime, float secondTime)
        => firstTime <= secondTime ? secondTime - firstTime : totalDuration - firstTime + secondTime;

    private void SnapshotTransitionState(int layer, float transitionDuration)
    {
        // This will look very similar to SetBoneTransform because we're basically snapshotting the currently rendered transforms.
        if (transitionDuration <= 0 || !state.Layers[layer].Animation.HasValue)
        {
            state.Layers[layer].TransitionTotalDuration = 0;
            state.Layers[layer].TransitionRemainingDuration = 0;
            state.Layers[layer].TransitionAnimation = null;
            return;
        }

        state.Layers[layer].TransitionTotalDuration = transitionDuration;
        state.Layers[layer].TransitionRemainingDuration = transitionDuration;
        state.Layers[layer].TransitionAnimation = state.Layers[layer].Animation;
        state.Layers[layer].TransitionTime = state.Layers[layer].Time;
        state.Layers[layer].TransitionPlaybackSpeed = state.Layers[layer].PlaybackSpeed;
    }
}

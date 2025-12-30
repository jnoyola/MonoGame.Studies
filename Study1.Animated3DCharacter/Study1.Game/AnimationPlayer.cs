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

    public void Restart(string animation, float playbackSpeed = 1.0f, bool loop = false)
    {
        state.Animation = animationSet.Get(animation);
        state.Time = 0;
        state.PlaybackSpeed = playbackSpeed;
        state.IsLooping = loop;
    }

    public void Play(string animation, float playbackSpeed = 1.0f, bool loop = false, float transitionDuration = 0.15f)
    {
        if (state.Animation.HasValue)
        {
            if (state.Animation.Value.Name == animation)
            {
                // It's the same animation. Update the playback settings.
                state.PlaybackSpeed = playbackSpeed;
                state.IsLooping = loop;
                return;
            }
            else
            {
                // It's a new animation, but we already had one playing. Transition from the previous one.
                SnapshotTransitionState(transitionDuration);
            }
        }

        Restart(animation, playbackSpeed, loop);
    }

    public void Stop()
    {
        state.Animation = null;
    }

    public void UpdateTime(GameTime gameTime)
    {
        if (!state.Animation.HasValue)
        {
            return;
        }

        state.Time += (float)gameTime.ElapsedGameTime.TotalSeconds * state.PlaybackSpeed;
        if (state.Time >= state.Animation.Value.DurationInSeconds)
        {
            if (state.IsLooping)
            {
                state.Time %= state.Animation.Value.DurationInSeconds;
            }
            else
            {
                state.Animation = null;
                return;
            }
        }
        else if (state.Time < 0)
        {
            if (state.IsLooping)
            {
                state.Time = state.Animation.Value.DurationInSeconds + (state.Time % state.Animation.Value.DurationInSeconds);
            }
            else
            {
                state.Animation = null;
                return;
            }
        }

        // Update the transition time if we're transitioning from a previous animation.
        if (state.TransitionRemainingDuration > 0)
        {
            state.TransitionRemainingDuration -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (state.TransitionRemainingDuration < 0)
            {
                state.TransitionTotalDuration = 0;
                state.TransitionRemainingDuration = 0;
                state.TransitionAnimation = null;
            }
        }
    }

    public void SetBoneTransform(Bone bone, out Matrix outMatrix)
    {
        if (!state.Animation.HasValue)
        {
            outMatrix = bone.LocalTransform;
            return;
        }

        // TODO: make this a predetermined mapping?
        var boneIndex = 0;
        while (state.Animation.Value.BoneChannels[boneIndex].BoneName != bone.Name)
        {
            ++boneIndex;
        }

        // If the animation doesn't have a channel for the requested bone, set the bone transform to be the bone's
        // unanimated transform by default. Note this doesn't apply the transition, and may look choppy.
        if (boneIndex == state.Animation.Value.BoneChannels.Count)
        {
            outMatrix = bone.LocalTransform;
            return;
        }

        var translationFrameIndex = FindFrameIndex(
            state.Animation.Value.BoneChannels[boneIndex].Translations.Keyframes,
            state.Time,
            state.PlaybackSpeed
        );
        var rotationFrameIndex = FindFrameIndex(
            state.Animation.Value.BoneChannels[boneIndex].Rotations.Keyframes,
            state.Time,
            state.PlaybackSpeed
        );
        var scaleFrameIndex = FindFrameIndex(
            state.Animation.Value.BoneChannels[boneIndex].Scales.Keyframes,
            state.Time,
            state.PlaybackSpeed
        );

        var translation = Sample(
            state.Animation.Value.BoneChannels[boneIndex].Translations.Keyframes,
            state.Animation.Value.DurationInSeconds,
            translationFrameIndex,
            state.Time,
            state.PlaybackSpeed
        );
        var rotation = Sample(
            state.Animation.Value.BoneChannels[boneIndex].Rotations.Keyframes,
            state.Animation.Value.DurationInSeconds,
            rotationFrameIndex,
            state.Time,
            state.PlaybackSpeed
        );
        var scale = Sample(
            state.Animation.Value.BoneChannels[boneIndex].Scales.Keyframes,
            state.Animation.Value.DurationInSeconds,
            scaleFrameIndex,
            state.Time,
            state.PlaybackSpeed
        );

        // If we're transitioning from a previous animation, interpolate between the transition snapshot and the current frame.
        if (state.TransitionRemainingDuration > 0 && state.TransitionAnimation.HasValue)
        {
            // TODO: make this a predetermined mapping?
            var transitionBoneIndex = 0;
            while (state.TransitionAnimation.Value.BoneChannels[transitionBoneIndex].BoneName != bone.Name)
            {
                ++transitionBoneIndex;
            }

            if (transitionBoneIndex < state.TransitionAnimation.Value.BoneChannels.Count)
            {
                var transitionTranslationFrameIndex = FindFrameIndex(
                    state.TransitionAnimation.Value.BoneChannels[transitionBoneIndex].Translations.Keyframes,
                    state.TransitionTime,
                    state.TransitionPlaybackSpeed
                );
                var transitionRotationFrameIndex = FindFrameIndex(
                    state.TransitionAnimation.Value.BoneChannels[transitionBoneIndex].Rotations.Keyframes,
                    state.TransitionTime,
                    state.TransitionPlaybackSpeed
                );
                var transitionScaleFrameIndex = FindFrameIndex(
                    state.TransitionAnimation.Value.BoneChannels[transitionBoneIndex].Scales.Keyframes,
                    state.TransitionTime,
                    state.TransitionPlaybackSpeed
                );

                var transitionTranslation = Sample(
                    state.TransitionAnimation.Value.BoneChannels[transitionBoneIndex].Translations.Keyframes,
                    state.TransitionAnimation.Value.DurationInSeconds,
                    transitionTranslationFrameIndex,
                    state.TransitionTime,
                    state.TransitionPlaybackSpeed
                );
                var transitionRotation = Sample(
                    state.TransitionAnimation.Value.BoneChannels[boneIndex].Rotations.Keyframes,
                    state.TransitionAnimation.Value.DurationInSeconds,
                    transitionRotationFrameIndex,
                    state.TransitionTime,
                    state.TransitionPlaybackSpeed
                );
                var transitionScale = Sample(
                    state.TransitionAnimation.Value.BoneChannels[boneIndex].Scales.Keyframes,
                    state.TransitionAnimation.Value.DurationInSeconds,
                    transitionScaleFrameIndex,
                    state.TransitionTime,
                    state.TransitionPlaybackSpeed
                );

                var transitionProgress = 1 - (state.TransitionRemainingDuration / state.TransitionTotalDuration);
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

    private void SnapshotTransitionState(float transitionDuration)
    {
        // This will look very similar to SetBoneTransform because we're basically snapshotting the currently rendered transforms.
        if (transitionDuration <= 0 || !state.Animation.HasValue)
        {
            state.TransitionTotalDuration = 0;
            state.TransitionRemainingDuration = 0;
            state.TransitionAnimation = null;
            return;
        }

        state.TransitionTotalDuration = transitionDuration;
        state.TransitionRemainingDuration = transitionDuration;
        state.TransitionAnimation = state.Animation;
        state.TransitionTime = state.Time;
        state.TransitionPlaybackSpeed = state.PlaybackSpeed;
    }
}

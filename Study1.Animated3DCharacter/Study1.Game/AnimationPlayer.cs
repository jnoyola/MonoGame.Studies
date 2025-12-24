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
    private Animation? animation = null;
    private float time = 0;
    private float playbackSpeed = 0;
    private bool isLooping = false;
    private readonly AnimationChannelState[] channelStates = new AnimationChannelState[animationSet.BoneChannelCount];
    private float transitionTotalDuration = 0;
    private float transitionRemainingDuration = 0;
    private Animation? transitionAnimation;
    private readonly AnimationChannelSnapshot[] transitionChannelSnapshots = new AnimationChannelSnapshot[animationSet.BoneChannelCount];

    public void Restart(string animation, float playbackSpeed = 1.0f, bool loop = false)
    {
        this.animation = animationSet.Get(animation);
        time = 0;
        this.playbackSpeed = playbackSpeed;
        isLooping = loop;
        for (int channelIndex = 0; channelIndex < channelStates.Length; ++channelIndex)
        {
            channelStates[channelIndex].TranslationFrameIndex = 0;
            channelStates[channelIndex].RotationFrameIndex = 0;
            channelStates[channelIndex].ScaleFrameIndex = 0;
        }
    }

    public void Play(string animation, float playbackSpeed = 1.0f, bool loop = false, float transitionDuration = 0.15f)
    {
        if (this.animation.HasValue)
        {
            if (this.animation.Value.Name == animation)
            {
                // It's the same animation. Update the playback settings.
                this.playbackSpeed = playbackSpeed;
                isLooping = loop;
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
        animation = null;
    }

    public void UpdateTime(GameTime gameTime)
    {
        if (!animation.HasValue)
        {
            return;
        }

        time += (float)gameTime.ElapsedGameTime.TotalSeconds * playbackSpeed;
        if (time >= animation.Value.DurationInSeconds)
        {
            if (isLooping)
            {
                time %= animation.Value.DurationInSeconds;
            }
            else
            {
                animation = null;
                return;
            }
        }
        else if (time < 0)
        {
            if (isLooping)
            {
                time = animation.Value.DurationInSeconds + (time % animation.Value.DurationInSeconds);
            }
            else
            {
                animation = null;
                return;
            }
        }

        for (int channelIndex = 0; channelIndex < animation.Value.BoneChannels.Count; ++channelIndex)
        {
            channelStates[channelIndex].TranslationFrameIndex = GetTargetFrameIndex(
                animation.Value.BoneChannels[channelIndex].Translations.Keyframes,
                channelStates[channelIndex].TranslationFrameIndex,
                time,
                playbackSpeed
            );
            channelStates[channelIndex].RotationFrameIndex = GetTargetFrameIndex(
                animation.Value.BoneChannels[channelIndex].Rotations.Keyframes,
                channelStates[channelIndex].RotationFrameIndex,
                time,
                playbackSpeed
            );
            channelStates[channelIndex].ScaleFrameIndex = GetTargetFrameIndex(
                animation.Value.BoneChannels[channelIndex].Scales.Keyframes,
                channelStates[channelIndex].ScaleFrameIndex,
                time,
                playbackSpeed
            );
        }

        // Update the transition time if we're transitioning from a previous animation.
        if (transitionRemainingDuration > 0)
        {
            transitionRemainingDuration -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (transitionRemainingDuration < 0)
            {
                transitionTotalDuration = 0;
                transitionRemainingDuration = 0;
                transitionAnimation = null;
            }
        }
    }

    public void SetBoneTransform(Bone bone, out Matrix outMatrix)
    {
        if (!animation.HasValue)
        {
            outMatrix = bone.LocalTransform;
            return;
        }

        // TODO: make this a predetermined mapping?
        var boneIndex = 0;
        while (animation.Value.BoneChannels[boneIndex].BoneName != bone.Name)
        {
            ++boneIndex;
        }

        // If the animation doesn't have a channel for the requested bone, set the bone transform to be the bone's
        // unanimated transform by default. Note this doesn't apply the transition, and may look choppy.
        if (boneIndex == animation.Value.BoneChannels.Count)
        {
            outMatrix = bone.LocalTransform;
            return;
        }

        var channelState = channelStates[boneIndex];
        var translation = Interpolate(
            animation.Value.BoneChannels[boneIndex].Translations.Keyframes,
            animation.Value.DurationInSeconds,
            channelState.TranslationFrameIndex,
            time,
            playbackSpeed
        );
        var rotation = Interpolate(
            animation.Value.BoneChannels[boneIndex].Rotations.Keyframes,
            animation.Value.DurationInSeconds,
            channelState.RotationFrameIndex,
            time,
            playbackSpeed
        );
        var scale = Interpolate(
            animation.Value.BoneChannels[boneIndex].Scales.Keyframes,
            animation.Value.DurationInSeconds,
            channelState.ScaleFrameIndex,
            time,
            playbackSpeed
        );

        // If we're transitioning from a previous animation, interpolate between the transition snapshot and the current frame.
        if (transitionRemainingDuration > 0 && transitionAnimation.HasValue)
        {
            // TODO: make this a predetermined mapping?
            var prevBoneIndex = 0;
            while (transitionAnimation.Value.BoneChannels[prevBoneIndex].BoneName != bone.Name)
            {
                ++prevBoneIndex;
            }

            if (prevBoneIndex < transitionAnimation.Value.BoneChannels.Count)
            {
                var transitionProgress = 1 - (transitionRemainingDuration / transitionTotalDuration);
                translation = Interpolate(transitionChannelSnapshots[prevBoneIndex].Translation, translation, transitionProgress);
                rotation = Interpolate(transitionChannelSnapshots[prevBoneIndex].Rotation, rotation, transitionProgress);
                scale = Interpolate(transitionChannelSnapshots[prevBoneIndex].Scale, scale, transitionProgress);
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

    private static ushort GetTargetFrameIndex<T>(
        Keyframe<T>[] keyframes, ushort currentFrameIndex, float targetTime, float playbackSpeed
    ) where T : struct
    {
        if (playbackSpeed > 0)
        {
            if (keyframes[currentFrameIndex].Time > targetTime && keyframes[0].Time <= targetTime)
            {
                currentFrameIndex = 0;
            }
            while (currentFrameIndex < keyframes.Length - 1 && keyframes[currentFrameIndex + 1].Time <= targetTime)
            {
                ++currentFrameIndex;
            }
        }
        else if (playbackSpeed < 0)
        {
            if (keyframes[currentFrameIndex].Time < targetTime && keyframes[keyframes.Length - 1].Time >= targetTime)
            {
                currentFrameIndex = (ushort)(keyframes.Length - 1);
            }
            while (currentFrameIndex > 0 && keyframes[currentFrameIndex - 1].Time >= targetTime)
            {
                --currentFrameIndex;
            }
        }

        return currentFrameIndex;
    }

    private static Vector3 Interpolate(Keyframe<Vector3>[] keyframes, float totalDuration, ushort frameIndex, float currentTime, float playbackSpeed)
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

    private static Quaternion Interpolate(Keyframe<Quaternion>[] keyframes, float totalDuration, ushort frameIndex, float currentTime, float playbackSpeed)
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
        if (transitionDuration <= 0 || !animation.HasValue)
        {
            transitionTotalDuration = 0;
            transitionRemainingDuration = 0;
            transitionAnimation = null;
            return;
        }

        transitionTotalDuration = transitionDuration;
        transitionRemainingDuration = transitionDuration;
        transitionAnimation = animation;
        for (int boneIndex = 0; boneIndex < animation.Value.BoneChannels.Count; ++boneIndex)
        {
            var channelState = channelStates[boneIndex];
            transitionChannelSnapshots[boneIndex].Translation = Interpolate(
                animation.Value.BoneChannels[boneIndex].Translations.Keyframes,
                animation.Value.DurationInSeconds,
                channelState.TranslationFrameIndex,
                time,
                playbackSpeed
            );
            transitionChannelSnapshots[boneIndex].Rotation = Interpolate(
                animation.Value.BoneChannels[boneIndex].Rotations.Keyframes,
                animation.Value.DurationInSeconds,
                channelState.RotationFrameIndex,
                time,
                playbackSpeed
            );
            transitionChannelSnapshots[boneIndex].Scale = Interpolate(
                animation.Value.BoneChannels[boneIndex].Scales.Keyframes,
                animation.Value.DurationInSeconds,
                channelState.ScaleFrameIndex,
                time,
                playbackSpeed
            );
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Study1.ContentFramework.Models;

namespace Study1.ContentFramework.Readers;

public class AnimationReader : ContentTypeReader<Animation>
{
    protected override Animation Read(ContentReader input, Animation existingInstance)
    {
        var name = input.ReadString();
        var durationInSeconds = input.ReadSingle();
        var wrapMode = (WrapMode)input.ReadUInt32();

        var boneCount = input.ReadUInt32();
        var boneIndexMapping = new int[boneCount];
        for (int boneIndex = 0; boneIndex < boneCount; ++boneIndex)
        {
            boneIndexMapping[boneIndex] = input.ReadInt32();
        }

        BoneChannel<Vector3>[]? translationChannels = null;
        var translationChannelCount = input.ReadUInt32();
        if (translationChannelCount > 0)
        {
            translationChannels = new BoneChannel<Vector3>[translationChannelCount];
            for (int channelIndex = 0; channelIndex < translationChannelCount; ++channelIndex)
            {
                var boneIndex = input.ReadInt32();
                var keyframeCount = input.ReadUInt32();
                var keyframes = new Keyframe<Vector3>[keyframeCount];
                for (int frameIndex = 0; frameIndex < keyframeCount; ++frameIndex)
                {
                    var frameTime = input.ReadSingle();
                    var frameValue = input.ReadVector3();
                    keyframes[frameIndex] = new Keyframe<Vector3>(frameTime, frameValue);
                }

                translationChannels[channelIndex] = new BoneChannel<Vector3>
                {
                    BoneIndex = boneIndex,
                    Keyframes = keyframes,
                };
            }
        }

        BoneChannel<Quaternion>[]? rotationChannels = null;
        var rotationChannelCount = input.ReadUInt32();
        if (rotationChannelCount > 0)
        {
            rotationChannels = new BoneChannel<Quaternion>[rotationChannelCount];
            for (int channelIndex = 0; channelIndex < rotationChannelCount; ++channelIndex)
            {
                var boneIndex = input.ReadInt32();
                var keyframeCount = input.ReadUInt32();
                var keyframes = new Keyframe<Quaternion>[keyframeCount];
                for (int frameIndex = 0; frameIndex < keyframeCount; ++frameIndex)
                {
                    var frameTime = input.ReadSingle();
                    var frameValue = input.ReadQuaternion();
                    keyframes[frameIndex] = new Keyframe<Quaternion>(frameTime, frameValue);
                }

                rotationChannels[channelIndex] = new BoneChannel<Quaternion>
                {
                    BoneIndex = boneIndex,
                    Keyframes = keyframes,
                };
            }
        }

        BoneChannel<Vector3>[]? scaleChannels = null;
        var scaleChannelCount = input.ReadUInt32();
        if (scaleChannelCount > 0)
        {
            scaleChannels = new BoneChannel<Vector3>[scaleChannelCount];
            for (int channelIndex = 0; channelIndex < scaleChannelCount; ++channelIndex)
            {
                var boneIndex = input.ReadInt32();
                var keyframeCount = input.ReadUInt32();
                var keyframes = new Keyframe<Vector3>[keyframeCount];
                for (int frameIndex = 0; frameIndex < keyframeCount; ++frameIndex)
                {
                    var frameTime = input.ReadSingle();
                    var frameValue = input.ReadVector3();
                    keyframes[frameIndex] = new Keyframe<Vector3>(frameTime, frameValue);
                }

                scaleChannels[channelIndex] = new BoneChannel<Vector3>
                {
                    BoneIndex = boneIndex,
                    Keyframes = keyframes,
                };
            }
        }

        return new Animation
        {
            Name = name,
            DurationInSeconds = durationInSeconds,
            WrapMode = wrapMode,
            BoneIndexMapping = boneIndexMapping,
            TranslationChannels = translationChannels,
            RotationChannels = rotationChannels,
            ScaleChannels = scaleChannels,
        };
    }
}

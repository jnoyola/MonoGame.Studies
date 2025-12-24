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

        var boneChannelCount = input.ReadUInt32();
        var boneChannels = new BoneChannel[boneChannelCount];
        for (int boneChannelIndex = 0; boneChannelIndex < boneChannelCount; ++boneChannelIndex)
        {
            var boneName = input.ReadString();

            var translationCount = input.ReadUInt32();
            var translationFrames = new Keyframe<Vector3>[translationCount];
            for (int frameIndex = 0; frameIndex < translationCount; ++frameIndex)
            {
                var frameTime = input.ReadSingle();
                var frameValue = input.ReadVector3();
                translationFrames[frameIndex] = new Keyframe<Vector3>(frameTime, frameValue);
            }

            var rotationCount = input.ReadUInt32();
            var rotationFrames = new Keyframe<Quaternion>[rotationCount];
            for (int frameIndex = 0; frameIndex < rotationCount; ++frameIndex)
            {
                var frameTime = input.ReadSingle();
                var frameValue = input.ReadQuaternion();
                rotationFrames[frameIndex] = new Keyframe<Quaternion>(frameTime, frameValue);
            }

            var scaleCount = input.ReadUInt32();
            var scaleFrames = new Keyframe<Vector3>[scaleCount];
            for (int frameIndex = 0; frameIndex < scaleCount; ++frameIndex)
            {
                var frameTime = input.ReadSingle();
                var frameValue = input.ReadVector3();
                scaleFrames[frameIndex] = new Keyframe<Vector3>(frameTime, frameValue);
            }

            boneChannels[boneChannelIndex] = new BoneChannel(boneName, translationFrames, rotationFrames, scaleFrames);
        }

        return new Animation(name, durationInSeconds, boneChannels);
    }
}

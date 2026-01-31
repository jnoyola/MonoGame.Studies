using System.Security.Cryptography.X509Certificates;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Study1.ContentFramework.Models;
using Study1.ContentFramework.Readers;

namespace Study1.Content.Writers;

[ContentTypeWriter]
public class AnimationWriter : ContentTypeWriter<Animation>
{
    public override string GetRuntimeReader(TargetPlatform targetPlatform) =>
        typeof(AnimationReader).AssemblyQualifiedName ?? throw new Exception("Failed to find AnimationReader.");

    protected override void Write(ContentWriter output, Animation value)
    {
        output.Write(value.Name);
        output.Write(value.DurationInSeconds);
        output.Write((uint)value.WrapMode);

        output.Write((uint)value.BoneIndexMapping.Count);
        foreach (var boneIndex in value.BoneIndexMapping)
        {
            output.Write(boneIndex);
        }

        if (value.TranslationChannels == null)
        {
            output.Write((uint)0);
        }
        else
        {
            output.Write((uint)value.TranslationChannels.Length);
            foreach (var channel in value.TranslationChannels)
            {
                output.Write(channel.BoneIndex);

                output.Write((uint)channel.Keyframes.Length);
                foreach (var frame in channel.Keyframes)
                {
                    output.Write(frame.Time);
                    output.Write(frame.Value);
                }
            }
        }

        if (value.RotationChannels == null)
        {
            output.Write((uint)0);
        }
        else
        {
            output.Write((uint)value.RotationChannels.Length);
            foreach (var channel in value.RotationChannels)
            {
                output.Write(channel.BoneIndex);

                output.Write((uint)channel.Keyframes.Length);
                foreach (var frame in channel.Keyframes)
                {
                    output.Write(frame.Time);
                    output.Write(frame.Value);
                }
            }
        }

        if (value.ScaleChannels == null)
        {
            output.Write((uint)0);
        }
        else
        {
            output.Write((uint)value.ScaleChannels.Length);
            foreach (var channel in value.ScaleChannels)
            {
                output.Write(channel.BoneIndex);

                output.Write((uint)channel.Keyframes.Length);
                foreach (var frame in channel.Keyframes)
                {
                    output.Write(frame.Time);
                    output.Write(frame.Value);
                }
            }
        }
    }
}

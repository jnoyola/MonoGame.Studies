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

        output.Write((uint)value.BoneChannels.Count);
        foreach (var channel in value.BoneChannels)
        {
            output.Write(channel.BoneName);

            output.Write((uint)channel.Translations.Keyframes.Length);
            foreach (var frame in channel.Translations.Keyframes)
            {
                output.Write(frame.Time);
                output.Write(frame.Value);
            }

            output.Write((uint)channel.Rotations.Keyframes.Length);
            foreach (var frame in channel.Rotations.Keyframes)
            {
                output.Write(frame.Time);
                output.Write(frame.Value);
            }

            output.Write((uint)channel.Scales.Keyframes.Length);
            foreach (var frame in channel.Scales.Keyframes)
            {
                output.Write(frame.Time);
                output.Write(frame.Value);
            }
        }
    }
}

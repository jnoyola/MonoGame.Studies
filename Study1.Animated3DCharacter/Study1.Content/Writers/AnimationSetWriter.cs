using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Study1.ContentFramework.Models;
using Study1.ContentFramework.Readers;

namespace Study1.Content.Writers;

[ContentTypeWriter]
public class AnimationSetWriter : ContentTypeWriter<AnimationSet>
{
    public override string GetRuntimeReader(TargetPlatform targetPlatform) =>
        typeof(AnimationSetReader).AssemblyQualifiedName ?? throw new Exception("Failed to find AnimationSetReader.");

    protected override void Write(ContentWriter output, AnimationSet value)
    {
        output.Write((uint)value.Count);
        foreach (var (name, animation) in value)
        {
            output.Write(name);
            output.WriteObject(animation);
        }
    }
}

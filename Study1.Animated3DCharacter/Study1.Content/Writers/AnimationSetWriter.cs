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
        output.Write((uint)value.BoneCount);
        foreach (var (name, index) in value.GetBoneIndices())
        {
            output.Write(name);
            output.Write(index);
        }

        output.Write((uint)value.AnimationCount);
        foreach (var (name, animation) in value.GetAnimations())
        {
            output.Write(name);
            output.WriteObject(animation);
        }

        output.Write((uint)value.AnimationLayerDefinitions.OverrideLayerCount);
        foreach (var layer in value.AnimationLayerDefinitions.GetAllOverrideLayers())
        {
            output.WriteObject(layer);
        }

        output.Write((uint)value.AnimationLayerDefinitions.AdditiveLayerCount);
        foreach (var layer in value.AnimationLayerDefinitions.GetAllAdditiveLayers())
        {
            output.WriteObject(layer);
        }
    }
}

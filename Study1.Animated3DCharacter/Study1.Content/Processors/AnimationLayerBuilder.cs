using Study1.ContentFramework.Models;

namespace Study1.Content.Processors;

public readonly struct AnimationLayerBuilder(AnimationLayer identifier)
{
    public AnimationLayer Identifier { get; } = identifier;
    public BoneMaskBuilder? BoneMask { get; init; } = null;

    public void Build(SharpGLTF.Schema2.ModelRoot modelRoot, Dictionary<string, int> boneIndices, AnimationLayerDefinitions layers)
    {
        if (Identifier.ToString().StartsWith("Additive"))
        {
            layers.AddLayer(
                new AdditiveLayerDefinition
                {
                    Identifier = Identifier,
                    BoneMask = BoneMask?.Build(modelRoot, boneIndices),
                }
            );
        }
        else
        {
            layers.AddLayer(
                new OverrideLayerDefinition
                {
                    Identifier = Identifier,
                    BoneMask = BoneMask?.Build(modelRoot, boneIndices),
                }
            );
        }
    }
}

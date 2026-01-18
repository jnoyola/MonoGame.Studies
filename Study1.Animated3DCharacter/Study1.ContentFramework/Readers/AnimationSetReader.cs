using Microsoft.Xna.Framework.Content;
using Study1.ContentFramework.Models;

namespace Study1.ContentFramework.Readers;

public class AnimationSetReader : ContentTypeReader<AnimationSet>
{
    protected override AnimationSet Read(ContentReader input, AnimationSet existingInstance)
    {
        var boneCount = input.ReadUInt32();
        var boneIndices = new Dictionary<string, int>((int)boneCount);
        for (int i = 0; i < boneCount; ++i)
        {
            var name = input.ReadString();
            var index = input.ReadInt32();
            boneIndices[name] = index;
        }

        var animationCount = input.ReadUInt32();
        var animationDict = new Dictionary<string, Animation>((int)animationCount);
        for (int i = 0; i < animationCount; ++i)
        {
            var name = input.ReadString();
            var animation = input.ReadObject<Animation>();
            animationDict[name] = animation;
        }
        
        var animationLayers = new AnimationLayerDefinitions();

        var overrideLayerCount = input.ReadUInt32();
        for (int i = 0; i < overrideLayerCount; ++i)
        {
            var layer = input.ReadObject<OverrideLayerDefinition>();
            animationLayers.AddLayer(layer);
        }

        var additiveLayerCount = input.ReadUInt32();
        for (int i = 0; i < additiveLayerCount; ++i)
        {
            var layer = input.ReadObject<AdditiveLayerDefinition>();
            animationLayers.AddLayer(layer);
        }

        return new AnimationSet(boneIndices, animationDict, animationLayers);
    }
}

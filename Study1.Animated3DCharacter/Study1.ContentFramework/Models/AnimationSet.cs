using System.Collections;

namespace Study1.ContentFramework.Models;

public readonly struct AnimationSet
{
    private readonly IReadOnlyDictionary<string, int> _boneIndices;
    private readonly IReadOnlyDictionary<string, Animation> _animations;

    public AnimationSet(IReadOnlyDictionary<string, int> boneIndices, IReadOnlyDictionary<string, Animation> animations, AnimationLayerDefinitions animationLayers)
    {
        _boneIndices = boneIndices;
        _animations = animations;
        AnimationLayerDefinitions = animationLayers;

        // TODO: move this to content writer.
        BoneChannelCount = 0;
        foreach (var animation in animations.Values)
        {
            if (animation.BoneChannels.Count > BoneChannelCount)
            {
                BoneChannelCount = animation.BoneChannels.Count;
            }
        }
    }

    public readonly int BoneCount => _boneIndices.Count;

    public readonly int AnimationCount => _animations.Count;

    public readonly int BoneChannelCount { get; }

    public readonly AnimationLayerDefinitions AnimationLayerDefinitions { get; }

    public readonly bool TryGetBoneIndex(string boneName, out int index) => _boneIndices.TryGetValue(boneName, out index);

    public readonly Animation GetAnimation(string key) => _animations[key];

    public readonly IEnumerable<KeyValuePair<string, int>> GetBoneIndices() => _boneIndices;

    public readonly IEnumerable<KeyValuePair<string, Animation>> GetAnimations() => _animations;
}

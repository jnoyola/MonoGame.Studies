namespace Study1.ContentFramework.Models;

public readonly struct AnimationSet
{
    private readonly IReadOnlyDictionary<string, Animation> _animations;

    public AnimationSet(IReadOnlyDictionary<string, Animation> animations, AnimationLayerDefinitions animationLayers)
    {
        _animations = animations;
        AnimationLayerDefinitions = animationLayers;
    }

    public readonly int AnimationCount => _animations.Count;

    public readonly AnimationLayerDefinitions AnimationLayerDefinitions { get; }

    public readonly Animation GetAnimation(string key) => _animations[key];

    public readonly IEnumerable<KeyValuePair<string, Animation>> GetAnimations() => _animations;
}

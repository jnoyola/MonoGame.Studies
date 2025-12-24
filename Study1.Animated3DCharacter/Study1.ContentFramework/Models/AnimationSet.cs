using System.Collections;

namespace Study1.ContentFramework.Models;

public readonly struct AnimationSet : IEnumerable<KeyValuePair<string, Animation>>
{
    private readonly IReadOnlyDictionary<string, Animation> _animations;

    public AnimationSet(IReadOnlyDictionary<string, Animation> animations)
    {
        _animations = animations;

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

    public readonly int Count => _animations.Count;

    public readonly int BoneChannelCount { get; }

    public readonly Animation Get(string key) => _animations[key];

    public readonly IEnumerator<KeyValuePair<string, Animation>> GetEnumerator() => _animations.GetEnumerator();

    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

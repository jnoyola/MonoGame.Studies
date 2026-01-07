using System.Diagnostics;

namespace Study1.ContentFramework.Models;

[DebuggerDisplay("{Name} ({BoneChannels.Count} channels, {DurationInSeconds} seconds)")]
public readonly struct Animation(
    string name,
    float durationInSeconds,
    IReadOnlyList<BoneChannel> boneChannels,
    AnimationLayerIdentifier defaultLayer
)
{
    public string Name => name;
    public float DurationInSeconds => durationInSeconds;
    public IReadOnlyList<BoneChannel> BoneChannels => boneChannels;
    public AnimationLayerIdentifier DefaultLayer => defaultLayer;
}

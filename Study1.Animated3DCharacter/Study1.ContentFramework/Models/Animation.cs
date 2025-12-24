using System.Diagnostics;

namespace Study1.ContentFramework.Models;

[DebuggerDisplay("{Name} ({ChannelCount} channels, {DurationInSeconds} seconds)")]
public readonly struct Animation(string name, float durationInSeconds, IReadOnlyList<BoneChannel> boneChannels)
{
    public string Name => name;
    public float DurationInSeconds => durationInSeconds;
    public IReadOnlyList<BoneChannel> BoneChannels => boneChannels;
}

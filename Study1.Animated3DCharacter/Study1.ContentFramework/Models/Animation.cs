using System.Diagnostics;

namespace Study1.ContentFramework.Models;

public enum WrapMode
{
    Once,
    Loop,
    Clamp,
}

[DebuggerDisplay("{Name} ({BoneChannels.Count} channels, {DurationInSeconds} seconds)")]
public class Animation
{
    public required string Name { get; init; }
    public required float DurationInSeconds { get; init; }
    public required WrapMode WrapMode { get; init; }
    public required IReadOnlyList<int> BoneIndexMapping { get; init; }
    public required IReadOnlyList<BoneChannel> BoneChannels { get; init; }
    public required AnimationLayer DefaultLayer { get; init; }  // TODO: remove DefaultLayer
}

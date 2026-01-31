using System.Diagnostics;
using Microsoft.Xna.Framework;

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
    public required BoneChannel<Vector3>[]? TranslationChannels { get; init; }
    public required BoneChannel<Quaternion>[]? RotationChannels { get; init; }
    public required BoneChannel<Vector3>[]? ScaleChannels { get; init; }
}

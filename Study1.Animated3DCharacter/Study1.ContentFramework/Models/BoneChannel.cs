using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Study1.ContentFramework.Models;

[DebuggerDisplay("Channel for {BoneName} ({Scales.Count} scales, {Rotations.Count} rotations, {Positions.Count} positions)")]
public readonly struct BoneChannel
{
    public BoneChannel(
        string boneName,
        Keyframe<Vector3>[] translationFrames,
        Keyframe<Quaternion>[] rotationFrames,
        Keyframe<Vector3>[] scaleFrames
    )
    {
        BoneName = boneName;
        Translations = new ChannelComponent<Vector3>(translationFrames);
        Rotations = new ChannelComponent<Quaternion>(rotationFrames);
        Scales = new ChannelComponent<Vector3>(scaleFrames);
    }

    public string BoneName { get; }
    public ChannelComponent<Vector3> Translations { get; }
    public ChannelComponent<Quaternion> Rotations { get; }
    public ChannelComponent<Vector3> Scales { get; }
}

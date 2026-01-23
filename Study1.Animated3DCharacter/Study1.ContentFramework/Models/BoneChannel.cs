using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Study1.ContentFramework.Models;

[DebuggerDisplay("Channel for {BoneName} ({Scales.Count} scales, {Rotations.Count} rotations, {Positions.Count} positions)")]
public readonly struct BoneChannel
{
    public BoneChannel(  // TODO (1.1): remove constructor and make props required.
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

    // TODO (optimize): channels can all be independent. Each can specify which bone index and transform type it performs.
    //      Then reverse the animation process so it operates over the channels instead of bones. This can reduce memory usage
    //      since many channels (especially translation and scale) may not exist for most animations.
    public string BoneName { get; }
    public ChannelComponent<Vector3> Translations { get; }
    public ChannelComponent<Quaternion> Rotations { get; }
    public ChannelComponent<Vector3> Scales { get; }
}

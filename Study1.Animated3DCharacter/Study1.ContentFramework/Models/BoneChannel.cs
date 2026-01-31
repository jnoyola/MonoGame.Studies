using System.Diagnostics;

namespace Study1.ContentFramework.Models;

[DebuggerDisplay("Channel {BoneIndex} ({typeof(T).Name,nq}[{Keyframes.Length}])")]
public readonly struct BoneChannel<T> where T : struct
{
    public required int BoneIndex { get; init; }
    public required Keyframe<T>[] Keyframes { get; init; }
}

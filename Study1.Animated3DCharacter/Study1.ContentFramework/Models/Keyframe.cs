using System.Diagnostics;

namespace Study1.ContentFramework.Models;

[DebuggerDisplay("Time: {Time} Value: {Value}")]
public readonly struct Keyframe<T>(float time, T value)
{
    public readonly float Time = time;
    public readonly T Value = value;
}

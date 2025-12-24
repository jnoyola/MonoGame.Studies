namespace Study1.ContentFramework.Models;

public readonly struct ChannelComponent<T>(Keyframe<T>[] keyframes) where T : struct
{
    public Keyframe<T>[] Keyframes => keyframes;
}

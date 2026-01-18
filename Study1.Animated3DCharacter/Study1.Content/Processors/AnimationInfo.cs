using Study1.ContentFramework.Models;

namespace Study1.Content.Processors;

[Flags]
public enum FreezeRootBoneOption
{
    None = 0,
    X = 1 << 0,
    Y = 1 << 1,
    Z = 1 << 2,
    XY = X | Y,
    XZ = X | Z,
    YZ = Y | Z,
    XYZ = X | Y | Z,
}

public struct AnimationInfo
{
    public required string Name { get; set; }
    public required WrapMode WrapMode { get; set; }
    public required AnimationLayer DefaultLayer { get; set; }
    public FreezeRootBoneOption FreezeRootBone { get; set; }
}

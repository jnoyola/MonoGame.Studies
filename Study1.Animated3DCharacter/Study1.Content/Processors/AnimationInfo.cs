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
    public string Name { get; set; }
    public FreezeRootBoneOption FreezeRootBone { get; set; }
    public AnimationLayerIdentifier DefaultLayer { get; set; }
}

using Microsoft.Xna.Framework;
using Study1.ContentFramework.Math;

namespace Study1.ContentFramework.Models;

public struct Bone
{
    public required string Name;
    public required int ParentIndex;
    public required Transform LocalTransform;
    public required Matrix InverseBindMatrix;
}

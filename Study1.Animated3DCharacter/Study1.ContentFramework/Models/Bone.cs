using Microsoft.Xna.Framework;

namespace Study1.ContentFramework.Models;

public class Bone(string name, int parentIndex, Matrix localTransform, Matrix inverseBindMatrix)
{
    public string Name => name;
    public int ParentIndex => parentIndex;
    public Matrix LocalTransform => localTransform;
    public Matrix InverseBindMatrix => inverseBindMatrix;
}

using Microsoft.Xna.Framework;

namespace Study1.ContentFramework.Math;

public struct Transform
{
    public static readonly Transform Identity = new()
    {
        Translation = Vector3.Zero,
        Rotation = Quaternion.Identity,
        Scale = Vector3.One
    };

    public Vector3 Translation;
    public Quaternion Rotation;
    public Vector3 Scale;

    public static void FromMatrix(in Matrix matrix, out Transform transform)
    {
        matrix.Decompose(out transform.Scale, out transform.Rotation, out transform.Translation);
    }

    public readonly Matrix ToMatrix(out Matrix matrix)
    {
        matrix = Matrix.CreateScale(Scale) * Matrix.CreateFromQuaternion(Rotation);
        matrix.Translation = Translation;
        return matrix;
    }
}

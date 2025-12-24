using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Study1.ContentFramework.Models;

public struct SkinnedTexturedVertexData : IVertexType 
{
    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0),
        new VertexElement(16, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0),
        new VertexElement(32, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(44, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0)
    );

    /// <summary> The layout of this vertex data, compatible with the default MonoGame SkinnedEffect. </summary>
    readonly VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    /// <summary> The position of the vertex itself. </summary>
    public Vector3 Position { get; init; }

    /// <summary> The packed bone indices that affect this vertex's position. </summary>
    public Byte4 BoneIndices { get; set; }

    /// <summary> The amount that each bone affects the final position of this vertex. </summary>
    public Vector4 BoneWeights { get; set; }

    /// <summary> The normal direction of this vertex. </summary>
    public Vector3 Normal { get; init; }

    /// <summary> The UV texture coordinate of this vertex. </summary>
    public Vector2 TextureCoordinate { get; init; }

    public readonly int GetBoneCount()
    {
        // Count the number of non-zero weights and return the result.
        int boneCount = 0;
        if (BoneWeights.X != 0) ++boneCount;
        if (BoneWeights.Y != 0) ++boneCount;
        if (BoneWeights.Z != 0) ++boneCount;
        if (BoneWeights.W != 0) ++boneCount;
        return boneCount;
    }

    /// <summary> Sets the next value of <see cref="BoneIndices"/> and <see cref="BoneWeights"/> to the given values. </summary>
    /// <param name="boneIndex"> The index of the next bone influence. </param>
    /// <param name="weight"> The weight of the next bone influence. </param>
    public void SetNextWeight(int boneIndex, float weight)
    {
        // Unpack the indices and copy the weights.
        Vector4 boneIndices = BoneIndices.ToVector4();
        Vector4 boneWeights = BoneWeights;

        // Calculate the bone count.
        int boneCount = GetBoneCount();

        // Set the index and weight.
        switch (boneCount)
        {
            case 0: boneIndices.X = boneIndex; boneWeights.X = weight; break;
            case 1: boneIndices.Y = boneIndex; boneWeights.Y = weight; break;
            case 2: boneIndices.Z = boneIndex; boneWeights.Z = weight; break;
            case 3: boneIndices.W = boneIndex; boneWeights.W = weight; break;
            default: throw new Exception("Cannot use more than 4 bones per vertex.");
        }

        // Set the blend indices and weights.
        BoneIndices = new Byte4(boneIndices);
        BoneWeights = boneWeights;
    }
}

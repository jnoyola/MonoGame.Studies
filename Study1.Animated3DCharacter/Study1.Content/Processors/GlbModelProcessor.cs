using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Study1.Content.WritableContent;
using Study1.ContentFramework.Models;

namespace Study1.Content.Processors;

[ContentProcessor(DisplayName = "GLB Model - MonoGame.Studies")]
public class GlbModelProcessor : ContentProcessor<SharpGLTF.Schema2.ModelRoot, WritableModel>
{
    public enum ColoringStyleOption
    {
        VertexColors,
        Texture,
    }

    /// <summary>
    /// Whether to reverse the polygon index winding order (i.e. flip polygons).
    /// </summary>
    public bool ReverseIndexWinding { get; set; } = false;

    /// <summary>
    /// What method the model uses to render color.
    /// </summary>
    public ColoringStyleOption ColoringStyle { get; set; } = ColoringStyleOption.VertexColors;

    /// <summary>
    /// Whether to include animations from this GLB file. If not set, the model is expected to be animated from a separate GLB file containing animations.
    /// </summary>
    public bool IncludeAnimations { get; set; } = false;

    public override WritableModel Process(SharpGLTF.Schema2.ModelRoot input, ContentProcessorContext context)
    {
        var meshes = new List<WritableMesh>();
        foreach (var mesh in input.LogicalMeshes)
        {
            foreach (var primitive in mesh.Primitives)
            {
                if (primitive.DrawPrimitiveType != SharpGLTF.Schema2.PrimitiveType.TRIANGLES)
                {
                    throw new Exception("GlbModelProcessor only support primitives rendered as TRIANGLES.");
                }

                var vertexBuffer = ColoringStyle switch
                {
                    ColoringStyleOption.VertexColors => CreateColoredVertexBuffer(primitive),
                    ColoringStyleOption.Texture => CreateTexturedVertexBuffer(primitive),
                    _ => throw new NotImplementedException(),
                };

                var indexBuffer = new IndexCollection();
                indexBuffer.AddRange(primitive.GetIndices().Select(i => (int)i));
                if (ReverseIndexWinding)
                {
                    for (var i = 0; i < indexBuffer.Count; i += 3)
                    {
                        var first = indexBuffer[i];
                        var last = indexBuffer[i + 2];
                        indexBuffer[i] = last;
                        indexBuffer[i + 2] = first;
                    }
                }

                meshes.Add(
                    new WritableMesh($"{mesh.Name}{primitive.LogicalIndex}", vertexBuffer, indexBuffer)
                );
            }
        }

        if (input.LogicalSkins.Count != 1)
        {
            throw new Exception("GlbModelProcessor requires exactly one armature.");
        }
        var armature = input.LogicalSkins[0];

        var nodeToBoneIndex = new Dictionary<SharpGLTF.Schema2.Node, int>();
        for (int boneIndex = 0; boneIndex < armature.JointsCount; ++boneIndex)
        {
            var node = armature.Joints[boneIndex];
            nodeToBoneIndex[node] = boneIndex;
        }

        var bones = new Bone[armature.JointsCount];
        for (int boneIndex = 0; boneIndex < bones.Length; ++boneIndex)
        {
            var node = armature.Joints[boneIndex];

            if (node.VisualParent == null || !nodeToBoneIndex.TryGetValue(node.VisualParent, out var parentIndex))
            {
                parentIndex = -1;
            }

            // Convert the matrices into MonoGame format. Remember once that is done, transforms are applied in reverse order.
            var localTransform = (Matrix)node.LocalTransform.Matrix;
            var inverseBindMatrix = (Matrix)armature.InverseBindMatrices[boneIndex];
            if (node.VisualParent == node.VisualRoot)
            {
                // If the bone is the root bone, apply the armature's transformation.
                var parentTransform = (Matrix)node.VisualParent.LocalTransform.Matrix;
                localTransform *= parentTransform;
            }

            bones[boneIndex] = new Bone(node.Name, parentIndex, localTransform, inverseBindMatrix);
        }

        AnimationSet? animations = null;
        if (IncludeAnimations)
        {
            // TODO: parse animations the same way the AnimationSetProcessor does.
            // TODO: AnimationWriter
            // TODO: AnimationReader
        }

        return new WritableModel(meshes, bones, animations);
    }

    private static VertexBufferContent CreateColoredVertexBuffer(SharpGLTF.Schema2.MeshPrimitive primitive)
    {
        var vertexPositions = primitive.GetVertices("POSITION").AsVector3Array();
        var vertexNormals = primitive.GetVertices("NORMAL").AsVector3Array();
        var vertexColors = primitive.GetVertices("COLOR_0").AsColorArray();
        var vertexJoints = primitive.GetVertices("JOINTS_0").AsVector4Array();
        var vertexWeights = primitive.GetVertices("WEIGHTS_0").AsVector4Array();
        var vertices = new SkinnedColoredVertexData[vertexPositions.Count];
        for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
        {
            vertices[vertexIndex] = new SkinnedColoredVertexData
            {
                Position = vertexPositions[vertexIndex],
                Normal = vertexNormals[vertexIndex],
                Color = new Byte4(vertexColors[vertexIndex] * 255),
                BoneIndices = new Byte4(vertexJoints[vertexIndex]),
                BoneWeights = vertexWeights[vertexIndex],
            };
        }

        return CreateVertexBuffer(SkinnedColoredVertexData.VertexDeclaration, vertices);
    }

    private static VertexBufferContent CreateTexturedVertexBuffer(SharpGLTF.Schema2.MeshPrimitive primitive)
    {
        var vertexPositions = primitive.GetVertices("POSITION").AsVector3Array();
        var vertexNormals = primitive.GetVertices("NORMAL").AsVector3Array();
        var vertexTexCoords = primitive.GetVertices("TEXCOORD_0").AsVector2Array();
        var vertexJoints = primitive.GetVertices("JOINTS_0").AsVector4Array();
        var vertexWeights = primitive.GetVertices("WEIGHTS_0").AsVector4Array();
        var vertices = new SkinnedTexturedVertexData[vertexPositions.Count];
        for (int vertexIndex = 0; vertexIndex < vertices.Length; ++vertexIndex)
        {
            vertices[vertexIndex] = new SkinnedTexturedVertexData
            {
                Position = vertexPositions[vertexIndex],
                Normal = vertexNormals[vertexIndex],
                TextureCoordinate = vertexTexCoords[vertexIndex],
                BoneIndices = new Byte4(vertexJoints[vertexIndex]),
                BoneWeights = vertexWeights[vertexIndex],
            };
        }

        return CreateVertexBuffer(SkinnedTexturedVertexData.VertexDeclaration, vertices);
    }

    private static VertexBufferContent CreateVertexBuffer<TVertex>(VertexDeclaration vertexDeclaration, TVertex[] vertices)
    {
        var vertexBuffer = new VertexBufferContent(vertices.Length);
        vertexBuffer.VertexDeclaration = new VertexDeclarationContent();
        vertexBuffer.VertexDeclaration.VertexStride = vertexDeclaration.VertexStride;
        foreach (var vertexElement in vertexDeclaration.GetVertexElements())
        {
            vertexBuffer.VertexDeclaration.VertexElements.Add(vertexElement);
        }
        vertexBuffer.Write(0, vertexDeclaration.VertexStride, vertices);
        return vertexBuffer;
    }
}

using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Study1.ContentFramework.Models;
using Model = Study1.ContentFramework.Models.Model;

namespace Study1.ContentFramework.Readers;

public class ModelReader : ContentTypeReader<Model>
{
    protected override Model Read(ContentReader input, Model existingInstance)
    {
        var meshCount = input.ReadUInt32();
        var meshes = new Mesh[meshCount];
        for (int i = 0; i < meshCount; ++i)
        {
            var name = input.ReadString();
            var mesh = new Mesh(name);
            input.ReadSharedResource(delegate (VertexBuffer v)
            {
                mesh.VertexBuffer = v;
            });
            input.ReadSharedResource(delegate (IndexBuffer v)
            {
                mesh.IndexBuffer = v;
            });

            meshes[i] = mesh;
        }

        var boneCount = input.ReadUInt32();
        var bones = new Bone[boneCount];
        for (int i = 0; i < boneCount; ++i)
        {
            var name = input.ReadString();
            var parentIndex = input.ReadInt32();
            var localTransform = input.ReadMatrix();
            var inverseBindMatrix = input.ReadMatrix();
            bones[i] = new Bone(name, parentIndex, localTransform, inverseBindMatrix);
        }

        var hasAnimations = input.ReadBoolean();
        AnimationSet? animations = hasAnimations ? input.ReadObject<AnimationSet>() : null;

        return new Model(meshes, bones, animations);
    }
}

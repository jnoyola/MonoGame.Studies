using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Study1.Content.WritableContent;
using Study1.ContentFramework.Readers;

namespace Study1.Content.Writers;

[ContentTypeWriter]
public class ModelWriter : ContentTypeWriter<WritableModel>
{
    public override string GetRuntimeReader(TargetPlatform targetPlatform) =>
        typeof(ModelReader).AssemblyQualifiedName ?? throw new Exception("Failed to find ModelReader.");

    protected override void Write(ContentWriter output, WritableModel value)
    {
        output.Write((uint)value.Meshes.Count);
        foreach (var mesh in value.Meshes)
        {
            output.Write(mesh.Name);
            output.WriteSharedResource(mesh.VertexBuffer);
            output.WriteSharedResource(mesh.IndexBuffer);
        }

        output.Write((uint)value.Bones.Count);
        foreach (var bone in value.Bones)
        {
            output.Write(bone.Name);
            output.Write(bone.ParentIndex);
            output.Write(bone.LocalTransform.Translation);
            output.Write(bone.LocalTransform.Rotation);
            output.Write(bone.LocalTransform.Scale);
            output.Write(bone.InverseBindMatrix);
        }

        output.Write(value.Animations != null);
        if (value.Animations != null)
        {
            output.WriteObject(value.Animations);
        }
    }
}

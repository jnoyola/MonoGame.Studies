using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace Study1.Content.WritableContent;

/// <summary>
/// An intermediate data structure to store writable pipeline content for <see cref="ContentFramework.Models.Mesh"/>.
/// </summary>
public class WritableMesh(string name, VertexBufferContent vertexBuffer, IndexCollection indexBuffer)
{
    public string Name => name;

    public VertexBufferContent VertexBuffer => vertexBuffer;

    public IndexCollection IndexBuffer => indexBuffer;
}

using Microsoft.Xna.Framework.Graphics;

namespace Study1.ContentFramework.Models;

public class Mesh(string name)
{
    private IndexBuffer? _indexBuffer;

    public string Name => name;
    public VertexBuffer? VertexBuffer { get; set; }
    public IndexBuffer? IndexBuffer
    {
        get => _indexBuffer;
        set
        {
            _indexBuffer = value;
            PrimitiveCount = _indexBuffer is null ? 0 : (_indexBuffer.IndexCount / 3);
        }
    }
    public int PrimitiveCount { get; private set; }
}

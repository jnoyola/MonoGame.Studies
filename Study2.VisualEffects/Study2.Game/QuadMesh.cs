using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Study2.Game;

public class QuadMesh
{

    public QuadMesh(GraphicsDevice graphicsDevice)
    {
        VertexPosition[] vertices = 
        [
            new VertexPosition(new Vector3(-0.5f, 0.5f, 0)),
            new VertexPosition(new Vector3(0.5f, 0.5f, 0)),
            new VertexPosition(new Vector3(-0.5f, -0.5f, 0)),
            new VertexPosition(new Vector3(0.5f, -0.5f, 0)),
        ];
        VertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPosition), vertices.Length, BufferUsage.WriteOnly);
        VertexBuffer.SetData(vertices);

        short[] indices = [0, 2, 1, 3];
        IndexBuffer = new IndexBuffer(graphicsDevice, typeof(short), indices.Length, BufferUsage.WriteOnly);
        IndexBuffer.SetData(indices);
    }

    public VertexBuffer VertexBuffer { get; }
    public IndexBuffer IndexBuffer { get; }
    public int PrimitiveCount => 2;
}

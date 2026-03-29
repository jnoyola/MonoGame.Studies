using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Study1.Game;

public class Grid
{
    private readonly BasicEffect _effect;
    private readonly VertexBuffer _vertexBuffer;
    private readonly int _primitiveCount;

    public Grid(GraphicsDevice graphicsDevice, uint width, Color? color = null)
    {
        _effect = new BasicEffect(graphicsDevice)
        {
            VertexColorEnabled = true,
            World = Matrix.Identity
        };

        var vertexColor = color ?? Color.Black;
        var vertices = new VertexPositionColor[(width + 1) * 2 * 2];
        var half = (float)width / 2;
        for (int i = 0; i <= width; ++i)
        {
            vertices[4 * i + 0] = new VertexPositionColor(new Vector3(-half + i, 0, -half), vertexColor);
            vertices[4 * i + 1] = new VertexPositionColor(new Vector3(-half + i, 0, half), vertexColor);
            vertices[4 * i + 2] = new VertexPositionColor(new Vector3(-half, 0, -half + i), vertexColor);
            vertices[4 * i + 3] = new VertexPositionColor(new Vector3(half, 0, -half + i), vertexColor);
        }

        _vertexBuffer = new VertexBuffer(graphicsDevice, typeof(VertexPositionColor), vertices.Length, BufferUsage.WriteOnly);
        _vertexBuffer.SetData(vertices);

        _primitiveCount = vertices.Length / 2;
    }

    public Matrix WorldMatrix
    {
        get => _effect.World;
        set => _effect.World = value;
    }

    public void Draw(Matrix cameraView, Matrix cameraProjection)
    {
        _effect.View = cameraView;
        _effect.Projection = cameraProjection;
        _effect.GraphicsDevice.SetVertexBuffer(_vertexBuffer);
        for (int effectPassIndex = 0; effectPassIndex < _effect.CurrentTechnique.Passes.Count; ++effectPassIndex)
        {
            _effect.CurrentTechnique.Passes[effectPassIndex].Apply();
            _effect.GraphicsDevice.DrawPrimitives(PrimitiveType.LineList, 0, _primitiveCount);
        }
    }
}

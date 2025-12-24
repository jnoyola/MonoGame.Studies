using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Study1.Game;

public class Grid
{
    private readonly BasicEffect _effect;
    private readonly VertexPositionColor[] _vertices;

    public Grid(GraphicsDevice graphicsDevice, uint width, Color? color = null)
    {
        _effect = new BasicEffect(graphicsDevice);
        _effect.VertexColorEnabled = true;

        var vertexColor = color ?? Color.Black;
        _vertices = new VertexPositionColor[(width + 1) * 2 * 2];
        var half = (float)width / 2;
        for (int i = 0; i <= width; ++i)
        {
            _vertices[4 * i + 0] = new VertexPositionColor(new Vector3(-half + i, 0, -half), vertexColor);
            _vertices[4 * i + 1] = new VertexPositionColor(new Vector3(-half + i, 0, half), vertexColor);
            _vertices[4 * i + 2] = new VertexPositionColor(new Vector3(-half, 0, -half + i), vertexColor);
            _vertices[4 * i + 3] = new VertexPositionColor(new Vector3(half, 0, -half + i), vertexColor);
        }
    }

    public Matrix WorldMatrix { get; set; } = Matrix.Identity;

    public void Draw(Matrix cameraView, Matrix cameraProjection)
    {
        _effect.World = WorldMatrix;
        _effect.View = cameraView;
        _effect.Projection = cameraProjection;

        for (int effectPassIndex = 0; effectPassIndex < _effect.CurrentTechnique.Passes.Count; ++effectPassIndex)
        {
            _effect.CurrentTechnique.Passes[effectPassIndex].Apply();
            _effect.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _vertices, 0, _vertices.Length / 2);
        }
    }
}

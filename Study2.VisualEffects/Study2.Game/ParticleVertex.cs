using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;

namespace Study2.Game;

public struct Particle
{
    public required readonly short EmitterId { get; init; }
    public required float Speed { get; set; }
    public required float RotationSpeed { get; set; }
    public float Age { get; set; }
    public required float Lifetime { get; set; }

    public required Vector2 SizeRandomFactor { get; set; }
    public required short ColorRandomFactorR { get; set; }
    public required short ColorRandomFactorG { get; set; }
    public required short ColorRandomFactorB { get; set; }
    public required short ColorRandomFactorA { get; set; }
}

public struct ParticleVertex : IVertexType 
{
    public static readonly VertexDeclaration VertexDeclaration = new(
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.TextureCoordinate, 1),
        new VertexElement(24, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 2),
        new VertexElement(28, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 3),
        new VertexElement(36, VertexElementFormat.Color, VertexElementUsage.Color, 0)
    );

    /// <summary> The layout of this vertex data, compatible with the default MonoGame SkinnedEffect. </summary>
    readonly VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

    /// <summary> The position of the particle. </summary>
    public required Vector3 Position;

    /// <summary> The orientation of the particle, used for velocity and axis alignment. </summary>
    public required Vector3 Orientation;

    /// <summary> The rotation of the particle in radians. </summary>
    public required float Rotation;

    /// <summary> The size of the particle. </summary>
    public required Vector2 Size;

    /// <summary> The color of the particle. </summary>
    public required Color Color;
}
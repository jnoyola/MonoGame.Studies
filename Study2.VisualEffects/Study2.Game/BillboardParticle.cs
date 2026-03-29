using Microsoft.Xna.Framework;

namespace Study2.Game;

public struct BillboardParticle
{
    public required readonly short EmitterId { get; init; }

    public required Vector3 Position;
    public required Vector3 Velocity;
    public required float Rotation;
    public required float RotationSpeed;
    public required Vector2 Size;
    public required Color Color;

    public required float Lifetime;
}

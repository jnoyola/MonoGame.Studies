using Microsoft.Xna.Framework;

namespace Study2.Game;

public struct ParticleEmitter
{
    public required bool IsActive;

    public required Vector3 Position;
    public Vector3 Velocity;

    public required Vector3 Direction;
    public required float Spread;
    public float EmissionFrequency;
    public required float EmissionRate
    {
        readonly get => 1f / EmissionFrequency;
        set => EmissionFrequency = 1f / value;
    }
    public float AccumulatedEmissionTime;

    public required float Speed;
    public float SpeedRandomness;
    public float SpeedChange;
    public float VelocityInheritanceFactor;
    public Vector3 WorldAcceleration;

    public float Rotation;
    public float RotationRandomness;
    public required float RotationSpeed;
    public float RotationSpeedRandomness;
    public float RotationSpeedChange;

    public required Vector2 SizeStart;
    public required Vector2 SizeEnd;
    public Vector2 SizeRandomness;

    public required Color ColorStart;
    public required Color ColorEnd;
    public Color ColorRandomness;

    public required float EdgeSoftness;
    public required float NoiseStrength;

    public required float Lifetime;
    public float LifetimeRandomness;
}

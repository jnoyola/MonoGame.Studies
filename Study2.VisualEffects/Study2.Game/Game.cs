using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Study2.Game;

namespace Study2.Game;

public class Game : Microsoft.Xna.Framework.Game
{
    private Profiler _profiler;
    private HeadsUpDisplay _hud;

    private Vector3 _cameraPosition;
    private Matrix _cameraView;
    private Matrix _cameraProjection;
    
    private Grid? _grid;

    private ParticleEmitter[] _particleEmitters;

    private Particle[] _particles;
    private ParticleVertex[] _particleVertices;
    private int _activeParticleCount;
    private Effect? _particleEffect;
    private QuadMesh? _quadMesh;
    private VertexBuffer? _particleVertexBuffer;
    private VertexBufferBinding[]? _particleVertexBuffers;

    public Game()
    {
        _ = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width,
            PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height,
            SynchronizeWithVerticalRetrace = false,  // Disable vsync because it can force FPS to be limited on some systems.
            IsFullScreen = false,
        };
        IsFixedTimeStep = false;
        IsMouseVisible = true;
        Content.RootDirectory = "Content";

        _profiler = new Profiler();
        _hud = new HeadsUpDisplay(_profiler, "Controls:\nEsc - Exit\n\nThis is a template. Clone it to start a new study.");

        var cameraTarget = new Vector3(0, 1f, 0);
        _cameraPosition = new Vector3(0, 20, -40);
        _cameraView = Matrix.CreateLookAt(_cameraPosition, cameraTarget, Vector3.Up);

        _particleEmitters = new ParticleEmitter[10];
        _particles = new Particle[100000];
        _particleVertices = new ParticleVertex[100000];
        _activeParticleCount = 0;

        _particleEmitters[0] = new ParticleEmitter
        {
            IsActive = true,
            Position = new Vector3(-5, 0, 0),
            Direction = Vector3.Up,
            Spread = 2.5f,
            Speed = 10f,
            SpeedRandomness = 1f,
            SpeedChange = -1f,
            WorldAcceleration = new Vector3(0, -0.05f, 0),
            RotationSpeed = 0,
            RotationSpeedRandomness = 0,
            RotationSpeedChange = 0,
            SizeStart = new Vector2(0.85f, 0.15f),
            SizeEnd = new Vector2(0.4f, 0.1f),
            SizeRandomness = new Vector2(0.1f, 0.1f),
            ColorStart = new Color(255, 150, 120, 255),
            ColorEnd = new Color(255, 80, 40, 255),
            ColorRandomness = new Color(20, 10, 10, 0),
            EdgeSoftness = 0.5f, // TODO: use
            NoiseStrength = 0.5f, // TODO: use
            Lifetime = 0.5f,
            LifetimeRandomness = 0.5f,
            EmissionRate = 2000,
        };

        _particleEmitters[2] = new ParticleEmitter
        {
            IsActive = true,
            Position = new Vector3(5, 0, 0),
            Direction = Vector3.Up,
            Spread = 3f,
            Speed = 4,
            SpeedRandomness = 1f,
            SpeedChange = -3f,
            WorldAcceleration = new Vector3(0, 0.01f, 0),
            RotationSpeed = 0,
            RotationSpeedRandomness = 0,
            RotationSpeedChange = 0,
            SizeStart = new Vector2(1, 1),
            SizeEnd = new Vector2(1, 1),
            SizeRandomness = new Vector2(0.1f, 0.1f),
            ColorStart = new Color(150, 180, 255, 150),
            ColorEnd = new Color(150, 180, 255, 150),
            ColorRandomness = new Color(10, 10, 20, 0),
            EdgeSoftness = 0.5f, // TODO: use
            NoiseStrength = 0.5f, // TODO: use
            Lifetime = 3,
            LifetimeRandomness = 0.5f,
            EmissionRate = 5,
        };
        _particleEmitters[3] = new ParticleEmitter
        {
            IsActive = true,
            Position = new Vector3(5, 0, 0),
            Direction = Vector3.Up,
            Spread = 2.5f,
            Speed = 4f,
            SpeedRandomness = 1f,
            SpeedChange = -1f,
            WorldAcceleration = new Vector3(0, -0.05f, 0),
            RotationSpeed = 0,
            RotationSpeedRandomness = 0,
            RotationSpeedChange = 0,
            SizeStart = new Vector2(0.15f, 0.15f),
            SizeEnd = new Vector2(0.1f, 0.1f),
            SizeRandomness = new Vector2(0.1f, 0.1f),
            ColorStart = new Color(120, 150, 255, 255),
            ColorEnd = new Color(120, 150, 255, 255),
            ColorRandomness = new Color(10, 10, 20, 0),
            EdgeSoftness = 0.5f, // TODO: use
            NoiseStrength = 0.5f, // TODO: use
            Lifetime = 0.5f,
            LifetimeRandomness = 0.5f,
            EmissionRate = 100,
        };
    }

    protected override void LoadContent()
    {
        _hud.LoadContent(Content, GraphicsDevice);

        _cameraProjection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(45),
            GraphicsDevice.Viewport.AspectRatio,
            0.1f,
            100f
        );

        _grid = new Grid(GraphicsDevice, 50);

        _particleEffect = Content.Load<Effect>("Effects/Particle");
        _quadMesh = new QuadMesh(GraphicsDevice);
        _particleVertexBuffer = new VertexBuffer(
            GraphicsDevice,
            typeof(ParticleVertex),
            _particles.Length,
            BufferUsage.WriteOnly
        );
        _particleVertexBuffers =
        [
            new VertexBufferBinding(_quadMesh.VertexBuffer),
            new VertexBufferBinding(_particleVertexBuffer, 0, 1)
        ];
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();
        if (keyboardState.IsKeyDown(Keys.Escape))
            Exit();

        UpdateParticleEmitters(gameTime);
        UpdateParticles((float)gameTime.ElapsedGameTime.TotalSeconds);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.SlateGray);
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;

        _grid?.Draw(_cameraView, _cameraProjection);

        RenderParticles();

        _hud.Draw(gameTime);
    }

    private void UpdateParticleEmitters(GameTime gameTime)
    {
        for (short emitterIndex = 0; emitterIndex < _particleEmitters.Length; ++emitterIndex)
        {
            ref var emitter = ref _particleEmitters[emitterIndex];
            if (!emitter.IsActive)
                continue;

            emitter.AccumulatedEmissionTime += (float)gameTime.ElapsedGameTime.TotalSeconds;
            while (emitter.AccumulatedEmissionTime >= emitter.EmissionFrequency)
            {
                SpawnBillboardParticle(in emitter, emitterIndex);
                emitter.AccumulatedEmissionTime -= emitter.EmissionFrequency;
            }
        }
    }

    private void SpawnBillboardParticle(in ParticleEmitter emitter, short emitterId)
    {
        if (_activeParticleCount >= _particles.Length)
        {
            return;
        }

        // Construct orientation from emitter direction and spread.
        var theta = Random.Shared.NextSingle() * MathHelper.TwoPi;
        var phi = (Random.Shared.NextSingle() - 0.5f) * emitter.Spread;
        var orientation = Vector3.Transform(emitter.Direction, Quaternion.CreateFromYawPitchRoll(theta, phi, 0));

        var sizeRandomFactor = emitter.SizeRandomness * (Random.Shared.NextSingle() - 0.5f);
        var colorRandomFactorR = (short)Random.Shared.Next(-emitter.ColorRandomness.R, emitter.ColorRandomness.R);
        var colorRandomFactorG = (short)Random.Shared.Next(-emitter.ColorRandomness.G, emitter.ColorRandomness.G);
        var colorRandomFactorB = (short)Random.Shared.Next(-emitter.ColorRandomness.B, emitter.ColorRandomness.B);
        var colorRandomFactorA = (short)Random.Shared.Next(-emitter.ColorRandomness.A, emitter.ColorRandomness.A);

        _particles[_activeParticleCount] = new Particle
        {
            EmitterId = emitterId,
            Speed = emitter.Speed + (emitter.SpeedRandomness * (Random.Shared.NextSingle() - 0.5f)),
            RotationSpeed = emitter.RotationSpeed + (emitter.RotationSpeedRandomness * (Random.Shared.NextSingle() - 0.5f)),
            Lifetime = emitter.Lifetime + (emitter.LifetimeRandomness * (Random.Shared.NextSingle() - 0.5f)),
            SizeRandomFactor = sizeRandomFactor,
            ColorRandomFactorR = colorRandomFactorR,
            ColorRandomFactorG = colorRandomFactorG,
            ColorRandomFactorB = colorRandomFactorB,
            ColorRandomFactorA = colorRandomFactorA,
        };
        _particleVertices[_activeParticleCount] = new ParticleVertex
        {
            Position = emitter.Position,
            Orientation = orientation,
            Rotation = emitter.Rotation + (emitter.RotationRandomness * (Random.Shared.NextSingle() - 0.5f)),
            Size = emitter.SizeStart + sizeRandomFactor,
            Color = new Color(
                emitter.ColorStart.R + colorRandomFactorR,
                emitter.ColorStart.G + colorRandomFactorG,
                emitter.ColorStart.B + colorRandomFactorB,
                emitter.ColorStart.A + colorRandomFactorA
            ),
        };
        ++_activeParticleCount;
    }

    private void UpdateParticles(float deltaTime)
    {
        for (int particleIndex = 0; particleIndex < _activeParticleCount; ++particleIndex)
        {
            ref var particle = ref _particles[particleIndex];
            ref var particleVertex = ref _particleVertices[particleIndex];
            particle.Age += deltaTime;

            if (particle.Age < particle.Lifetime)
            {
                ref var emitter = ref _particleEmitters[particle.EmitterId];
                var normalizedAge = particle.Age / particle.Lifetime;

                particle.Speed = Math.Max(0, particle.Speed + emitter.SpeedChange * deltaTime);
                var velocity = particleVertex.Orientation * particle.Speed;
                if (emitter.WorldAcceleration != Vector3.Zero)
                {
                    velocity += emitter.WorldAcceleration * particle.Age;
                    Vector3.Normalize(ref velocity, out particleVertex.Orientation);
                }
                particleVertex.Position += velocity * deltaTime;
                particleVertex.Rotation += particle.RotationSpeed * deltaTime;

                particleVertex.Size = Vector2.Lerp(emitter.SizeStart, emitter.SizeEnd, normalizedAge) + particle.SizeRandomFactor;
                var color = Color.Lerp(emitter.ColorStart, emitter.ColorEnd, normalizedAge);
                particleVertex.Color = new Color(
                    color.R + particle.ColorRandomFactorR,
                    color.G + particle.ColorRandomFactorG,
                    color.B + particle.ColorRandomFactorB,
                    color.A + particle.ColorRandomFactorA
                );
            }
            else
            {
                // Remove the particle by swapping it with the last active particle and reducing the count.
                --_activeParticleCount;
                _particles[particleIndex] = _particles[_activeParticleCount];
                _particleVertices[particleIndex] = _particleVertices[_activeParticleCount];
                // Stay on the same index to check the swapped particle.
                --particleIndex;
            }
        }
    }

    private void RenderParticles()
    {
        if (_particleEffect == null || _quadMesh == null || _particleVertexBuffer == null || _particleVertexBuffers == null || _activeParticleCount == 0)
        {
            return;
        }

        GraphicsDevice.BlendState = BlendState.AlphaBlend;
        GraphicsDevice.DepthStencilState = DepthStencilState.None;

        _particleEffect.Parameters["CameraPosition"].SetValue(_cameraPosition);
        _particleEffect.Parameters["ViewProj"].SetValue(_cameraView * _cameraProjection);

        // TODO: once we have different types of particles and need to filter, is it faster to copy chunks to the GPU,
        // or to filter the buffer on the CPU first and then copy the entire thing?
        _particleVertexBuffer.SetData(0, _particleVertices, 0, _activeParticleCount, ParticleVertex.VertexDeclaration.VertexStride);
        GraphicsDevice.SetVertexBuffers(_particleVertexBuffers);
        GraphicsDevice.Indices = _quadMesh.IndexBuffer;

        for (int effectPassIndex = 0; effectPassIndex < _particleEffect.CurrentTechnique.Passes.Count; ++effectPassIndex)
        {
            _particleEffect.CurrentTechnique.Passes[effectPassIndex].Apply();
            GraphicsDevice.DrawInstancedPrimitives(PrimitiveType.TriangleStrip, 0, 0, _quadMesh.PrimitiveCount, _activeParticleCount);
            _hud.CountPolygons(_quadMesh.PrimitiveCount * _activeParticleCount);
        }
    }
}

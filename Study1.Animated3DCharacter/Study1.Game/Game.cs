using System.Runtime.CompilerServices;
using Arch.Core;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Schedulers;
using Study1.ContentFramework.Models;
using Study1.ContentFramework.Readers;
using Model = Study1.ContentFramework.Models.Model;

namespace Study1.Game;

public class Game : Microsoft.Xna.Framework.Game
{
    private record struct Position(Matrix Transform);
    private record struct BoneTransforms(Matrix[] Matrices);
    private record struct CharacterState(int Row, int Col, bool IsRunning, bool IsWaving, bool IsHeadDown);

    private const int CharacterRowCount = 25;
    private const int CharacterColCount = 40;
    private const int CharacterCount = CharacterRowCount * CharacterColCount;
    private static readonly Random Random = new();

    private Profiler _profiler;
    private HeadsUpDisplay _hud;

    private float _cameraSpeed;
    private Matrix _cameraView;
    private Matrix _cameraProjection;
    
    private Grid? _grid;

    private World _world;
    private QueryDescription _stateQuery;
    private QueryDescription _animationQuery;
    private QueryDescription _drawQuery;

    private Model? _characterModel;
    private AnimationSet _animationSet;
    private Effect? _skinnedEffect;

    private bool _isHeadDown;
    KeyboardState _prevKeyboardState;

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
        _hud = new HeadsUpDisplay(_profiler, "Controls:\nWASD - Move camera\nR - Raise/Lower heads\nEsc - Exit");

        _cameraSpeed = 10f;
        var cameraTarget = new Vector3(0, 1f, 0);
        var cameraPosition = new Vector3(0, 20, -40);
        _cameraView = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);

        var ecsSchedulerConfig = new JobScheduler.Config
        {
            ThreadCount = Environment.ProcessorCount,
            ThreadPrefixName = "ECS",
        };
        _world = World.Create();
        World.SharedJobScheduler = new(ecsSchedulerConfig);
        AnimationSystem.Initialize(ecsSchedulerConfig.ThreadCount);
        _stateQuery = new QueryDescription().WithAll<CharacterState>();
        _animationQuery = new QueryDescription().WithAll<CharacterState, AnimationState, BoneTransforms>();
        _drawQuery = new QueryDescription().WithAll<Position, BoneTransforms>();

        // _animationPlayer = new AnimationPlayer();
    }

    protected override void Initialize()
    {
        base.Initialize();
        _prevKeyboardState = Keyboard.GetState();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _world.Dispose();
        World.SharedJobScheduler?.Dispose();
    }

    protected override void LoadContent()
    {
        ReaderRegistry.Initialize();

        _hud.LoadContent(Content, GraphicsDevice);

        _cameraProjection = Matrix.CreatePerspectiveFieldOfView(
            MathHelper.ToRadians(45),
            GraphicsDevice.Viewport.AspectRatio,
            0.1f,
            100f
        );

        _grid = new Grid(GraphicsDevice, 50);

        _characterModel = Content.Load<Model>("Models/man");
        var rowSpacing = 1.5f;
        var colSpacing = 1.0f;
        for (int rowIndex = 0; rowIndex < CharacterRowCount; ++rowIndex)
        {
            for (int colIndex = 0; colIndex < CharacterColCount; ++colIndex)
            {
                var transform = Matrix.CreateWorld(
                    new Vector3(
                        -(float)(CharacterColCount - 1) * colSpacing / 2 + colIndex * colSpacing, 
                        0, 
                        -(float)(CharacterRowCount - 1) * rowSpacing / 2 + rowIndex * rowSpacing
                    ),
                    Vector3.Backward,
                    Vector3.Up
                );
                _world.Create(
                    new Position(transform),
                    new CharacterState(rowIndex, colIndex, false, false, false),
                    new AnimationState(),
                    new BoneTransforms(new Matrix[_characterModel.Bones.Length])
                );
            }
        }

        _animationSet = Content.Load<AnimationSet>("Models/man_anims");

        _skinnedEffect = Content.Load<Effect>("Effects/SkinnedVertexColoredEffect");
        _skinnedEffect.Parameters["DiffuseColor"].SetValue(new Vector4(1f));
        _skinnedEffect.Parameters["AmbientColor"].SetValue(new Vector3(0.2f));
        _skinnedEffect.Parameters["SpecularPower"].SetValue(16);
        _skinnedEffect.Parameters["DirLight0Direction"].SetValue(new Vector3(-1, -1, 1));
        _skinnedEffect.Parameters["DirLight0DiffuseColor"].SetValue(new Vector3(0.43f, 0.4f, 0.4f));
        _skinnedEffect.Parameters["DirLight0SpecularColor"].SetValue(new Vector3(0));
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();
        if (keyboardState.IsKeyDown(Keys.Escape))
            Exit();

        // Control camera position.
        var cameraVelocity = Vector3.Zero;
        if (keyboardState.IsKeyDown(Keys.W))
        {
            cameraVelocity += Vector3.UnitZ;
        }
        if (keyboardState.IsKeyDown(Keys.S))
        {
            cameraVelocity -= Vector3.UnitZ;
        }
        if (keyboardState.IsKeyDown(Keys.A))
        {
            cameraVelocity += Vector3.UnitX;
        }
        if (keyboardState.IsKeyDown(Keys.D))
        {
            cameraVelocity -= Vector3.UnitX;
        }
        if (cameraVelocity != Vector3.Zero)
        {
            cameraVelocity.Normalize();
            cameraVelocity *= _cameraSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        if (keyboardState.IsKeyDown(Keys.R) && !_prevKeyboardState.IsKeyDown(Keys.R))
        {
            _isHeadDown = !_isHeadDown;
        }

        // Apply physics.
        _cameraView.Translation += cameraVelocity;

        _prevKeyboardState = keyboardState;

        // Perform state updates for all characters.
        var stateSystem = new StateSystem
        {
            GameTime = gameTime,
            IsHeadDown = _isHeadDown
        };
        _world.InlineQuery<StateSystem, CharacterState>(in _stateQuery, ref stateSystem);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.SlateGray);
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;

        _grid?.Draw(_cameraView, _cameraProjection);
        DrawCharacters(gameTime);

        _hud.Draw(gameTime);

        _profiler.ProfileDraw(gameTime);
    }

    private void DrawCharacters(GameTime gameTime)
    {
        if (_characterModel == null || _skinnedEffect == null)
        {
            return;
        }

        var animationSystem = new AnimationSystem
        {
            GameTime = gameTime,
            CharacterModel = _characterModel,
            AnimationSet = _animationSet,
        };
        _world.InlineParallelQuery<AnimationSystem, CharacterState, AnimationState, BoneTransforms>(in _animationQuery, ref animationSystem);

        var drawSystem = new DrawSystem
        {
            GraphicsDevice = GraphicsDevice,
            CameraView = _cameraView,
            CameraProjection = _cameraProjection,
            CharacterModel = _characterModel,
            Effect = _skinnedEffect,
            Hud = _hud,
        };
        _world.InlineQuery<DrawSystem, Position, BoneTransforms>(in _drawQuery, ref drawSystem);
    }

    private struct StateSystem : IForEach<CharacterState>
    {
        public required GameTime GameTime;
        public required bool IsHeadDown;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Update(ref CharacterState characterState)
        {
            // Set each character's head based on the global state.
            characterState.IsHeadDown = IsHeadDown;

            // Start each character running based on position.
            if (GameTime.TotalGameTime.TotalSeconds * 10 > characterState.Row + characterState.Col * 1.4)
            {
                characterState.IsRunning = true;
            }

            // Start each character waving with a random chance.
            if (characterState.IsWaving)
            {
                characterState.IsWaving = false;
            }
            else if (Random.NextSingle() < 1 / (float)CharacterCount)
            {
                characterState.IsWaving = true;
            }
        }
    }

    private struct AnimationSystem : IForEach<CharacterState, AnimationState, BoneTransforms>
    {
        private static int _workerCount;
        private static AnimationPlayer[]? _animationPlayers;

        [ThreadStatic]
        private static int _workerIndex;

        public required GameTime GameTime;
        public required Model CharacterModel;
        public required AnimationSet AnimationSet;

        public static void Initialize(int maxWorkerCount)
        {
            _animationPlayers = new AnimationPlayer[maxWorkerCount];
            for (int i = 0; i < _animationPlayers.Length; ++i)
            {
                _animationPlayers[i] = new();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(ref CharacterState characterState, ref AnimationState animationState, ref BoneTransforms boneTransforms)
        {
            if (_workerIndex == 0)
            {
                _workerIndex = ++_workerCount;
            }

            var animationPlayer = _animationPlayers![_workerIndex - 1];

            SelectAnimations(ref characterState, ref animationState);

            // Step 1: Initialize the bone transform array with animation transforms.
            AnimationPlayer.UpdateTime(ref animationState, GameTime);
            animationPlayer.SampleBones(ref animationState, ref AnimationSet, CharacterModel.Bones, boneTransforms.Matrices);

            // Step 2: Transform each bone transform by the chain of parent bone transforms.
            // Note this works because the bones are required to be in order, where parentIndex < i for all bones.
            // Remember that MonoGame, like most graphics systems, stores transformation matrices as rows instead of columns,
            // which means that matrix multiplication order is reversed from the mathematical representation to apply transforms.
            for (int i = 0; i < boneTransforms.Matrices.Length; ++i)
            {
                var parentIndex = CharacterModel.Bones[i].ParentIndex;
                if (parentIndex >= 0)
                {
                    boneTransforms.Matrices[i] *= boneTransforms.Matrices[parentIndex];
                }
            }

            // Step 3: Layer on an additional transformation to convert from local coordinate space to model coordinate space
            // using the inverse bind matrix.
            for (int i = 0; i < boneTransforms.Matrices.Length; ++i)
            {
                boneTransforms.Matrices[i] = CharacterModel.Bones[i].InverseBindMatrix * boneTransforms.Matrices[i];
            }
        }

        private readonly void SelectAnimations(ref CharacterState characterState, ref AnimationState animationState)
        {
            if (characterState.IsRunning)
            {
                AnimationPlayer.Play(ref animationState, AnimationSet, AnimationLayer.Base, "run_forward");
                AnimationPlayer.Play(ref animationState, AnimationSet, AnimationLayer.AdditiveBase, "hand_closed_left");
                AnimationPlayer.Play(ref animationState, AnimationSet, AnimationLayer.AdditiveBase, "hand_closed_right");
            }
            else
            {
                AnimationPlayer.Play(ref animationState, AnimationSet, AnimationLayer.Base, "idle");
            }

            if (characterState.IsWaving)
            {
                AnimationPlayer.Play(ref animationState, AnimationSet, AnimationLayer.UpperBody, "wave");
            }

            if (characterState.IsHeadDown)
            {
                AnimationPlayer.Play(ref animationState, AnimationSet, AnimationLayer.AdditiveBase, "head_down");
            }
        }
    }
    
    private struct DrawSystem : IForEach<Position, BoneTransforms>
    {
        public required GraphicsDevice GraphicsDevice;
        public required Matrix CameraView;
        public required Matrix CameraProjection;
        public required Model CharacterModel;
        public required Effect Effect;
        public required HeadsUpDisplay Hud;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Update(ref Position position, ref BoneTransforms boneTransforms) {
            var bonesParam = Effect.Parameters["Bones"];
            var worldParam = Effect.Parameters["World"];
            var worldTransposeParam = Effect.Parameters["WorldInverseTranspose"];
            var worldViewProjParam = Effect.Parameters["WorldViewProj"];
            bonesParam.SetValue(boneTransforms.Matrices);
            worldParam.SetValue(position.Transform);
            worldTransposeParam.SetValue(Matrix.Transpose(Matrix.Invert(position.Transform)));
            worldViewProjParam.SetValue(position.Transform * CameraView * CameraProjection);
            for (int meshIndex = 0; meshIndex < CharacterModel.Meshes.Length; ++meshIndex)
            {
                var mesh = CharacterModel.Meshes[meshIndex];
                GraphicsDevice.SetVertexBuffer(mesh.VertexBuffer);
                GraphicsDevice.Indices = mesh.IndexBuffer;

                for (int effectPassIndex = 0; effectPassIndex < Effect.CurrentTechnique.Passes.Count; ++effectPassIndex)
                {
                    Effect.CurrentTechnique.Passes[effectPassIndex].Apply();
                    GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mesh.PrimitiveCount);
                    Hud.CountPolygons(mesh.PrimitiveCount);
                }
            }
        }
    }
}

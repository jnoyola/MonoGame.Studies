using System.Collections;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Study1.ContentFramework.Models;
using Study1.ContentFramework.Readers;
using Model = Study1.ContentFramework.Models.Model;

namespace Study1.Game;

public class Game : Microsoft.Xna.Framework.Game
{
    private const int CharacterRowCount = 25;
    private const int CharacterColCount = 40;
    private const int CharacterCount = CharacterRowCount * CharacterColCount;
    private static readonly Random Random = new();

    private readonly GraphicsDeviceManager _graphics;

    private HeadsUpDisplay _hud;

    private float _cameraSpeed;
    private Matrix _cameraView;
    private Matrix _cameraProjection;
    
    private Grid? _grid;

    private Model? _characterModel;
    private Matrix[] _characterTransforms;
    private Matrix[][] _characterBoneTransforms;
    private AnimationSet _animationSet;
    private AnimationState[] _animationStates;
    private Effect? _skinnedEffect;

    private BitArray _isRunning;
    private BitArray _isWaving;
    private bool _isHeadDown;
    KeyboardState _prevKeyboardState;

    public Game()
    {
        _graphics = new GraphicsDeviceManager(this)
        {
            PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width,
            PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height,
            SynchronizeWithVerticalRetrace = false,  // Disable vsync because it can force FPS to be limited on some systems.
            IsFullScreen = true,
        };
        IsFixedTimeStep = false;
        IsMouseVisible = true;
        Content.RootDirectory = "Content";

        _hud = new HeadsUpDisplay("Controls:\nWASD - Move camera\nR - Raise/Lower heads\nEsc - Exit");

        _cameraSpeed = 10f;
        var cameraTarget = new Vector3(0, 1f, 0);
        var cameraPosition = new Vector3(0, 20, -40);
        _cameraView = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);

        _characterTransforms = new Matrix[CharacterCount];
        _characterBoneTransforms = new Matrix[CharacterCount][];
        _animationStates = new AnimationState[CharacterCount];
        _isRunning = new BitArray(CharacterCount);
        _isWaving = new BitArray(CharacterCount);

        var rowSpacing = 1.5f;
        var colSpacing = 1.0f;
        for (int rowIndex = 0; rowIndex < CharacterRowCount; ++rowIndex)
        {
            for (int colIndex = 0; colIndex < CharacterColCount; ++colIndex)
            {
                _characterTransforms[rowIndex * CharacterColCount + colIndex] = Matrix.CreateWorld(
                    new Vector3(
                        -(float)(CharacterColCount - 1) * colSpacing / 2 + colIndex * colSpacing, 
                        0, 
                        -(float)(CharacterRowCount - 1) * rowSpacing / 2 + rowIndex * rowSpacing
                    ),
                    Vector3.Backward,
                    Vector3.Up
                );
            }
        }
    }

    protected override void Initialize()
    {
        base.Initialize();
        _prevKeyboardState = Keyboard.GetState();
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
        for (int i = 0; i < CharacterCount; ++i)
        {
            _characterBoneTransforms[i] = new Matrix[_characterModel.Bones.Count];
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

        // Start each character running based on position.
        for (int rowIndex = 0; rowIndex < CharacterRowCount; ++rowIndex)
        {
            for (int colIndex = 0; colIndex < CharacterColCount; ++colIndex)
            {
                if (gameTime.TotalGameTime.TotalSeconds * 10 > rowIndex + colIndex * 1.4)
                {
                    _isRunning[rowIndex * CharacterColCount + colIndex] = true;
                }
            }
        }

        // Start one character waving each tick with a random chance.
        for (int i = 0; i < CharacterCount; ++i)
        {
            if (_isWaving[i])
            {
                _isWaving[i] = false;
                continue;
            }

            if (Random.NextSingle() < 1 / (float)CharacterCount)
            {
                _isWaving[i] = true;
                break;
            }
        }
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.SlateGray);
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;

        _grid?.Draw(_cameraView, _cameraProjection);
        for (int characterIndex = 0; characterIndex < CharacterCount; ++characterIndex)
        {
            DrawCharacter(characterIndex, gameTime, _cameraView);
        }

        _hud?.Draw(gameTime);
    }

    private void DrawCharacter(int characterIndex, GameTime gameTime, Matrix cameraView)
    {
        if (_characterModel == null || _characterBoneTransforms == null || _skinnedEffect == null)
        {
            return;
        }

        ref var animationState = ref _animationStates[characterIndex];
        ref var boneTransforms = ref _characterBoneTransforms[characterIndex];
        ref var characterTransform = ref _characterTransforms[characterIndex];

        // Select the correct animation.
        SelectAnimations(characterIndex, ref animationState);

        // Step 1: Initialize the bone transform array with animation transforms.
        AnimationPlayer.UpdateTime(ref animationState, gameTime);
        for (int i = 0; i < _characterModel.Bones.Count; ++i)
        {
            AnimationPlayer.SampleBone(ref animationState, ref _animationSet, _characterModel.Bones[i], out boneTransforms[i]);
        }

        // Step 2: Transform each bone transform by the chain of parent bone transforms.
        // Note this works because the bones are required to be in order, where parentIndex < i for all bones.
        // Remember that MonoGame, like most graphics systems, stores transformation matrices as rows instead of columns,
        // which means that matrix multiplication order is reversed from the mathematical representation to apply transforms.
        for (int i = 0; i < boneTransforms.Length; ++i)
        {
            var parentIndex = _characterModel.Bones[i].ParentIndex;
            if (parentIndex >= 0)
            {
                boneTransforms[i] *= boneTransforms[parentIndex];
            }
        }

        // Step 3: Layer on an additional transformation to convert from local coordinate space to model coordinate space
        // using the inverse bind matrix.
        for (int i = 0; i < boneTransforms.Length; ++i)
        {
            boneTransforms[i] = _characterModel.Bones[i].InverseBindMatrix * boneTransforms[i];
        }

        // Now draw all components of the model.
        _skinnedEffect.Parameters["Bones"].SetValue(boneTransforms);
        _skinnedEffect.Parameters["World"].SetValue(characterTransform);
        _skinnedEffect.Parameters["WorldInverseTranspose"].SetValue(Matrix.Transpose(Matrix.Invert(characterTransform)));
        _skinnedEffect.Parameters["WorldViewProj"].SetValue(characterTransform * cameraView * _cameraProjection);
        for (int meshIndex = 0; meshIndex < _characterModel.Meshes.Count; ++meshIndex)
        {
            var mesh = _characterModel.Meshes[meshIndex];
            GraphicsDevice.SetVertexBuffer(mesh.VertexBuffer);
            GraphicsDevice.Indices = mesh.IndexBuffer;

            for (int effectPassIndex = 0; effectPassIndex < _skinnedEffect.CurrentTechnique.Passes.Count; ++effectPassIndex)
            {
                _skinnedEffect.CurrentTechnique.Passes[effectPassIndex].Apply();
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, mesh.PrimitiveCount);
                _hud.CountPolygons(mesh.PrimitiveCount);
            }
        }
    }

    private void SelectAnimations(int characterIndex, ref AnimationState animationState)
    {
        if (_isRunning[characterIndex])
        {
            AnimationPlayer.Play(ref animationState, ref _animationSet, AnimationLayer.Base, "run_forward");
            AnimationPlayer.Play(ref animationState, ref _animationSet, AnimationLayer.AdditiveBase, "hand_closed_left");
            AnimationPlayer.Play(ref animationState, ref _animationSet, AnimationLayer.AdditiveBase, "hand_closed_right");
        }
        else
        {
            AnimationPlayer.Play(ref animationState, ref _animationSet, AnimationLayer.Base, "idle");
        }

        if (_isWaving[characterIndex])
        {
            AnimationPlayer.Play(ref animationState, ref _animationSet, AnimationLayer.UpperBody, "wave");
        }

        if (_isHeadDown)
        {
            AnimationPlayer.Play(ref animationState, ref _animationSet, AnimationLayer.AdditiveBase, "head_down");
        }
    }
}

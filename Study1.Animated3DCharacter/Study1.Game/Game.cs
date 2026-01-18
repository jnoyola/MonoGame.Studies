using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Study1.ContentFramework.Models;
using Study1.ContentFramework.Readers;
using Model = Study1.ContentFramework.Models.Model;

namespace Study1.Game;

public class Game : Microsoft.Xna.Framework.Game
{
    private readonly GraphicsDeviceManager _graphics;

    private HeadsUpDisplay _hud;

    private Matrix _cameraProjection;
    
    private Grid? _grid;

    private Matrix _characterTransform;
    private Vector3 _characterInstructedVelocity;
    private Vector3 _characterVelocity;
    private Vector3 _characterHeading;
    private float _characterSpeed;
    private Model? _characterModel;
    private Matrix[]? _characterBoneTransforms;
    private AnimationPlayer _animationPlayer;
    private Effect? _skinnedEffect;

    KeyboardState _prevKeyboardState;
    bool _isWaving;
    bool _isLeftHandClosed;
    bool _isRightHandClosed;
    bool _isHeadDown;
    bool _isBreathingHeavy;

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

        _hud = new HeadsUpDisplay("Controls:\nWASD - Move\nF - Wave\nQE - Open/Close hands\nR - Raise/Lower head\nT - Toggle heavy breathing\nEsc - Exit");

        _characterTransform = Matrix.Identity;
        _characterHeading = Vector3.UnitZ;
        _characterSpeed = 4f;
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

        _grid = new Grid(GraphicsDevice, 20);

        _characterModel = Content.Load<Model>("Models/man");
        _characterBoneTransforms = new Matrix[_characterModel.Bones.Count];
        var characterAnims = Content.Load<AnimationSet>("Models/man_anims");
        _animationPlayer = new AnimationPlayer(characterAnims);

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

        // Control character position.
        _characterInstructedVelocity = Vector3.Zero;
        if (keyboardState.IsKeyDown(Keys.W))
        {
            _characterInstructedVelocity += Vector3.UnitZ;
        }
        if (keyboardState.IsKeyDown(Keys.S))
        {
            _characterInstructedVelocity -= Vector3.UnitZ;
        }
        if (keyboardState.IsKeyDown(Keys.A))
        {
            _characterInstructedVelocity += Vector3.UnitX;
        }
        if (keyboardState.IsKeyDown(Keys.D))
        {
            _characterInstructedVelocity -= Vector3.UnitX;
        }
        if (_characterInstructedVelocity != Vector3.Zero)
        {
            _characterInstructedVelocity.Normalize();
            _characterHeading = _characterInstructedVelocity;
            _characterInstructedVelocity *= _characterSpeed * (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        _isWaving = keyboardState.IsKeyDown(Keys.F);
        if (keyboardState.IsKeyDown(Keys.Q) && !_prevKeyboardState.IsKeyDown(Keys.Q))
        {
            _isLeftHandClosed = !_isLeftHandClosed;
        }
        if (keyboardState.IsKeyDown(Keys.E) && !_prevKeyboardState.IsKeyDown(Keys.E))
        {
            _isRightHandClosed = !_isRightHandClosed;
        }
        if (keyboardState.IsKeyDown(Keys.R) && !_prevKeyboardState.IsKeyDown(Keys.R))
        {
            _isHeadDown = !_isHeadDown;
        }
        if (keyboardState.IsKeyDown(Keys.T) && !_prevKeyboardState.IsKeyDown(Keys.T))
        {
            _isBreathingHeavy = !_isBreathingHeavy;
        }

        // Apply physics.
        _characterTransform = Matrix.CreateWorld(_characterTransform.Translation, -_characterHeading, Vector3.Up);
        _characterVelocity = _characterInstructedVelocity;
        _characterTransform.Translation += _characterVelocity;

        _prevKeyboardState = keyboardState;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.SlateGray);
        GraphicsDevice.BlendState = BlendState.Opaque;
        GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        
        var cameraTarget = _characterTransform.Translation + new Vector3(0, 1f, 0);
        var cameraPosition = cameraTarget + new Vector3(0, 6, -8);
        var cameraView = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);

        _grid?.Draw(cameraView, _cameraProjection);
        DrawCharacter(gameTime, cameraView);

        _hud?.Draw(gameTime);
    }

    private void DrawCharacter(GameTime gameTime, Matrix cameraView)
    {
        if (_characterModel == null || _characterBoneTransforms == null || _skinnedEffect == null)
        {
            return;
        }

        // Select the correct animation.
        SelectAnimations();

        // Step 1: Initialize the bone transform array with animation transforms.
        _animationPlayer.UpdateTime(gameTime);
        for (int i = 0; i < _characterModel.Bones.Count; ++i)
        {
            _animationPlayer.SampleBone(_characterModel.Bones[i], out _characterBoneTransforms[i]);
        }

        // Step 2: Transform each bone transform by the chain of parent bone transforms.
        // Note this works because the bones are required to be in order, where parentIndex < i for all bones.
        // Remember that MonoGame, like most graphics systems, stores transformation matrices as rows instead of columns,
        // which means that matrix multiplication order is reversed from the mathematical representation to apply transforms.
        for (int i = 0; i < _characterBoneTransforms.Length; ++i)
        {
            var parentIndex = _characterModel.Bones[i].ParentIndex;
            if (parentIndex >= 0)
            {
                _characterBoneTransforms[i] *= _characterBoneTransforms[parentIndex];
            }
        }

        // Step 3: Layer on an additional transformation to convert from local coordinate space to model coordinate space
        // using the inverse bind matrix.
        for (int i = 0; i < _characterBoneTransforms.Length; ++i)
        {
            _characterBoneTransforms[i] = _characterModel.Bones[i].InverseBindMatrix * _characterBoneTransforms[i];
        }

        // Now draw all components of the model.
        _skinnedEffect.Parameters["Bones"].SetValue(_characterBoneTransforms);
        _skinnedEffect.Parameters["World"].SetValue(_characterTransform);
        _skinnedEffect.Parameters["WorldInverseTranspose"].SetValue(Matrix.Transpose(Matrix.Invert(_characterTransform)));
        _skinnedEffect.Parameters["WorldViewProj"].SetValue(_characterTransform * cameraView * _cameraProjection);
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

    private void SelectAnimations()
    {
        if (_characterInstructedVelocity == Vector3.Zero)
        {
            _animationPlayer.Play(AnimationLayer.Base, "idle");
        }
        else
        {
            _animationPlayer.Play(AnimationLayer.Base, "run_forward", playbackSpeed: _characterSpeed / 3);
        }

        if (_isWaving)
        {
            // This plays on a separate layer, and can be performed while idle or while running.
            _animationPlayer.Play(AnimationLayer.UpperBody, "wave", transitionDuration: 0.5f);
        }

        // These are all played on the additive layer, and therefore can stack up to the 3 slot capacity.
        if (_isLeftHandClosed)
        {
            _animationPlayer.Play(AnimationLayer.AdditiveBase, "hand_closed_left");
        }
        if (_isRightHandClosed)
        {
            _animationPlayer.Play(AnimationLayer.AdditiveBase, "hand_closed_right");
        }
        if (_isHeadDown)
        {
            _animationPlayer.Play(AnimationLayer.AdditiveBase, "head_down", transitionDuration: 0.5f);
        }
        if (_isBreathingHeavy)
        {
            // Since this additive animation is 4th in priority, and the additive layer is configured to have 3 slots,
            // this will get preempted if the other 3 additive animations are all being played. Notice that you always
            // see smooth fades when turning on and off additive animations, *unless* you have this one on and then
            // switch to having the other 3 higher priority animations on.
            _animationPlayer.Play(AnimationLayer.AdditiveBase, "breathe_heavy");
        }
    }
}

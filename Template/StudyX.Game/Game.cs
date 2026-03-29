using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace StudyX.Game;

public class Game : Microsoft.Xna.Framework.Game
{
    private Profiler _profiler;
    private HeadsUpDisplay _hud;

    private Matrix _cameraView;
    private Matrix _cameraProjection;
    
    private Grid? _grid;

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
        var cameraPosition = new Vector3(0, 20, -40);
        _cameraView = Matrix.CreateLookAt(cameraPosition, cameraTarget, Vector3.Up);
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
    }

    protected override void Update(GameTime gameTime)
    {
        var keyboardState = Keyboard.GetState();
        if (keyboardState.IsKeyDown(Keys.Escape))
            Exit();
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.SlateGray);

        _grid?.Draw(_cameraView, _cameraProjection);

        _hud.Draw(gameTime);
    }
}

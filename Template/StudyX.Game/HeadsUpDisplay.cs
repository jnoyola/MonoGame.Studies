using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace StudyX.Game;

public class HeadsUpDisplay(Profiler profiler, string instructions)
{
    private const float UpdateInterval = 0.25f;
    private const int TimestampBufferSize = 30;

    private readonly Process _proc = Process.GetCurrentProcess();
    private readonly StringBuilder _sb = new(instructions.Length + 256);
    private readonly double[] _timestamps = new double[TimestampBufferSize];
    private int _timestampIndex = 0;
    private double _lastUpdateTimestamp = 0;
    private double _fps = 0;
    private int _meshCount = 0;
    private int _polyCount = 0;

    private SpriteFont? _font;
    private SpriteBatch? _spriteBatch;

    public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
    {
        _font = content.Load<SpriteFont>("Fonts/Tahoma_14");
        _spriteBatch = new SpriteBatch(graphicsDevice);
    }

    public void CountPolygons(int polyCount)
    {
        ++_meshCount;
        _polyCount += polyCount;
    }

    public void Draw(GameTime gameTime)
    {
        // Get the FPS from the oldest timestamp.
        if (gameTime.TotalGameTime.TotalSeconds - _lastUpdateTimestamp >= UpdateInterval)
        {
            var dt = gameTime.TotalGameTime.TotalSeconds - _timestamps[_timestampIndex];
            _fps = _timestamps.Length / dt;
            _lastUpdateTimestamp = gameTime.TotalGameTime.TotalSeconds;
        }

        // Insert the new timestamp.
        _timestamps[_timestampIndex] = gameTime.TotalGameTime.TotalSeconds;
        if (++_timestampIndex == _timestamps.Length)
        {
            _timestampIndex = 0;
        }

        // Construct the text.
        _sb.Clear();
        _sb.Append("Time: ");
        if (gameTime.TotalGameTime.Minutes < 10)
        {
            _sb.Append('0');
        }
        _sb.Append(gameTime.TotalGameTime.Minutes);
        _sb.Append(':');
        if (gameTime.TotalGameTime.Seconds < 10)
        {
            _sb.Append('0');
        }
        _sb.Append(gameTime.TotalGameTime.Seconds);
        _sb.Append('.');
        if (gameTime.TotalGameTime.Milliseconds < 100)
        {
            _sb.Append('0');
            if (gameTime.TotalGameTime.Milliseconds < 10)
            {
                _sb.Append('0');
            }
        }
        _sb.Append(gameTime.TotalGameTime.Milliseconds);
        _sb.Append("\nFPS: ");
        _sb.Append((int)_fps);
        _sb.Append("\nMesh Count: ");
        _sb.Append(_meshCount);
        _sb.Append("\nPoly Count: ");
        _sb.Append(_polyCount);
        _sb.Append("\nProcess Mem: ");
        _sb.Append(_proc.WorkingSet64);
        _sb.Append("\nGC Total Mem: ");
        _sb.Append(profiler.TotalGcMemory);
        _sb.Append("\nGC Addl Mem: ");
        _sb.Append(profiler.AdditionalGcMemory);
        _sb.Append("\nGC Total Count: ");
        _sb.Append(profiler.TotalGcCount);
        _sb.Append("\nGC Addl Count: ");
        _sb.Append(profiler.AdditionalGcCount);
        _sb.Append("\n\n");
        _sb.Append(instructions);

        // Draw the display.
        if (_spriteBatch != null && _font != null)
        {
            _spriteBatch.Begin();
            _spriteBatch.DrawString(_font, _sb, new Vector2(10, 10), Color.White);
            _spriteBatch.End();
        }

        // Reset any per-frame state.
        _meshCount = 0;
        _polyCount = 0;
    }
}

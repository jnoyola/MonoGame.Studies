using Microsoft.Xna.Framework;

namespace Study1.Game;

public class Profiler
{
    /// <summary>
    /// The number of frames required to warm up profiling.
    /// </summary>
    private const long WarmUpDrawCount = 60;
    /// <summary>
    /// The number of additional seconds to warm up profiling. This is often an estimate on top of
    /// <see cref="WarmUpDrawCount"/> because the heap takes a short time to be expanded to account
    /// for memory allocations.
    /// </summary>
    private const double WarmUpExtraDelaySeconds = 0.5;

    private long _drawCount;
    private double _warmUpTargetTime;
    private bool _isInitialized;
    private long _initialGcMemory;
    private int _initialGcCount;
    private long _totalGcMemory;
    private int _totalGcCount;

    public long TotalGcMemory => _totalGcMemory;
    public long AdditionalGcMemory => _totalGcMemory - _initialGcMemory;
    public long TotalGcCount => _totalGcCount;
    public long AdditionalGcCount => _totalGcCount - _initialGcCount;

    public void ProfileDraw(GameTime gameTime)
    {
        ++_drawCount;
        if (_drawCount == WarmUpDrawCount)
        {
            _warmUpTargetTime = gameTime.TotalGameTime.TotalSeconds + WarmUpExtraDelaySeconds;
        }
        if (!_isInitialized && _warmUpTargetTime > 0 && gameTime.TotalGameTime.TotalSeconds > _warmUpTargetTime)
        {
            Initialize();
        }

        _totalGcMemory = GC.GetTotalMemory(false);
        _totalGcCount = GC.CollectionCount(GC.MaxGeneration);
    }

    private void Initialize()
    {
        _isInitialized = true;

        GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true);
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        _initialGcMemory = GC.GetTotalMemory(true);
        _initialGcCount = GC.CollectionCount(GC.MaxGeneration);
    }
}

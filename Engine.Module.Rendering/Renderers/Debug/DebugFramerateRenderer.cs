using System.Drawing;
using Engine.Core.Common;
using Engine.Core.Profiling;
using Engine.Module.Rendering.Utilities;

namespace Engine.Module.Rendering.Renderers.Debug;

public class DebugFramerateRenderer(RenderingHost host): IRenderer
{
    private readonly DebugTextGrid _textGrid = new(host);
    
    private readonly List<double> _frameTimes = [];
    private double _frameTimeAccumulator = 0.0;
    private int _framerate = 0;
    private int _onePercentLow = 0;
    private List<IProfilerEntry> _profilerEntries = [];
    
    // After profiler update, remember the number of frames the snapshot represents to calculate timing per frame
    private int _lastSeenFrameIndex = -1;
    private int _framesForLatestUpdate = 1;

    public void RenderFrame(double deltaTime)
    {
        UpdateFramerate(deltaTime);
        RenderFramerate();
    }

    public void RenderFrameWithTiming(double delta)
    {
        var stopwatch = Profiler.Start();
        RenderFrame(delta);
        stopwatch.StopAndReport(typeof(DebugProfilerRenderer), ProfilingContext.RenderingDebugFramerate);
    }

    private int _lastSeenProfilerBufferIndex = -1;
    private void RenderFramerate()
    {
        _textGrid.Draw(0, 0, DebugTextGrid.Anchor.TopRight, Color.White, "FPS: " + _framerate);
        _textGrid.Draw(0, 1, DebugTextGrid.Anchor.TopRight, Color.White, "1% Low: " + _onePercentLow);

        if (_lastSeenProfilerBufferIndex != Profiler.CurrentBufferIndex)
        {
            _framesForLatestUpdate = Math.Max(1, FrameCounter.Current - _lastSeenFrameIndex);
            _lastSeenFrameIndex = FrameCounter.Current;
            _profilerEntries = Profiler
                .Query(ProfilingContext.BackstageUpdate |
                       ProfilingContext.PhysicsUpdate |
                       ProfilingContext.RenderingPrepare |
                       ProfilingContext.RenderingDebugFramerate |
                       ProfilingContext.RenderingDebugLog |
                       ProfilingContext.RenderingDebugProfiler)
                .OrderBy(p => p.FullName)
                .ToList();
            _lastSeenProfilerBufferIndex = Profiler.CurrentBufferIndex;
        }
        var line = 2;
        const int maxEntriesRendered = 32;
        for (var i = 0; i < Math.Min(_profilerEntries.Count, maxEntriesRendered); i++)
        {
            var entry = _profilerEntries[i];
            var timePerFrame = entry.TotalMilliseconds / _framesForLatestUpdate;
            _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.White, $"{entry.TypeName}: {timePerFrame:F2}ms");
        }
    }
    
    private void UpdateFramerate(double deltaTime)
    {
        _frameTimeAccumulator += deltaTime;
        _frameTimes.Add(deltaTime);
        if (_frameTimeAccumulator < 0.1)
            return;
        
        var averageFrameTime = _frameTimes.Count > 0 ? _frameTimes.Average() : 0.0;
        var framerate = 1.0 / averageFrameTime;
        
        var onePercentCount = Math.Max(1, (int)Math.Ceiling(_frameTimes.Count * 0.01));
        var slowestFrames = _frameTimes.OrderByDescending(x => x).Take(onePercentCount).ToList();
        var onePercentLowFrameTime = slowestFrames.Count != 0 ? slowestFrames.Average() : 0.0;
        var onePercentLow = onePercentLowFrameTime > 0 ? 1.0 / onePercentLowFrameTime : 0.0;
        
        _framerate = (int)Math.Round(framerate);
        _onePercentLow = (int)Math.Round(onePercentLow);
        _frameTimes.Clear();
        _frameTimeAccumulator = 0.0;
    }
}
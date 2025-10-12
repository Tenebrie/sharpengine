using System.Drawing;
using Engine.Core.Common;
using Engine.Core.Profiling;
using Engine.Module.Rendering.Renderers.Fonts;
using Engine.Module.Rendering.Utilities;

namespace Engine.Module.Rendering.Renderers.Debug;

public class DebugProfilerFrameRenderer(RenderingHost host, TextRenderer textRenderer)
{
    private readonly DebugTextGrid _textGrid = new(host, textRenderer);
    
    private List<IProfilerEntry> _profilerEntries = [];

    private long _lastSeenFrameIndex = -1;
    private int _lastSeenProfilerBufferIndex = -1;
    private long _framesForLatestUpdate = 1;

    public void RenderFrame(double deltaTime)
    {
        if (_lastSeenProfilerBufferIndex != Profiler.CurrentBufferIndex)
        {
            _framesForLatestUpdate = Math.Max(1, FrameCounter.Current - _lastSeenFrameIndex);
            _profilerEntries = Profiler.QueryWorstOffenders().Take(10).ToList();
            _lastSeenFrameIndex = FrameCounter.Current;
            _lastSeenProfilerBufferIndex = Profiler.CurrentBufferIndex;
        }

        var line = 2;
        foreach (var entry in _profilerEntries)
        {
            var timePerFrame = entry.TotalMilliseconds / _framesForLatestUpdate;
            _textGrid.Draw(50, line++, DebugTextGrid.Anchor.TopLeft, Color.White, $"{entry.FullName}: {timePerFrame:F2}ms");
        }
    }

    public void RenderFrameWithTiming(double delta)
    {
        var stopwatch = Profiler.Start();
        RenderFrame(delta);
        stopwatch.StopAndReport(typeof(DebugProfilerFrameRenderer), ProfilingContext.RenderingDebugProfiler);
    }
}
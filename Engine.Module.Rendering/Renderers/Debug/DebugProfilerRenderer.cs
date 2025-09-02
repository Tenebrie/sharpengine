using System.Drawing;
using Engine.Core.Common;
using Engine.Core.Profiling;
using Engine.Module.Rendering.Utilities;

namespace Engine.Module.Rendering.Renderers.Debug;

public class DebugProfilerRenderer(RenderingHost host): IRenderer
{
    private readonly DebugTextGrid _textGrid = new(host);
    
    private List<IProfilerEntry> _profilerEntries = [];

    private int _lastSeenFrameIndex = -1;

    public void RenderFrame(double deltaTime)
    {
        var entries = Profiler.QueryWorstOffenders().Take(10).ToList();
        var line = 2;
        foreach (var entry in entries)
        {
            _textGrid.Draw(50, line++, DebugTextGrid.Anchor.TopLeft, Color.White, $"{entry.FullName}: {entry.AverageMilliseconds:F2}ms");
        }
    }

    public void RenderFrameWithTiming(double delta)
    {
        var stopwatch = Profiler.Start();
        RenderFrame(delta);
        stopwatch.StopAndReport(typeof(DebugProfilerRenderer), ProfilingContext.RenderingDebugProfiler);
    }
}
using System.Drawing;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Logging;
using Engine.Core.Profiling;
using Engine.Module.Rendering.Renderers.Fonts;
using Engine.Module.Rendering.Utilities;

namespace Engine.Module.Rendering.Renderers.Debug;

public class DebugFramerateFrameRenderer(RenderingHost host, TextRenderer textRenderer)
{
    private readonly DebugTextGrid _textGrid = new(host, textRenderer);
    
    private readonly List<double> _frameTimes = [];
    private double _frameTimeAccumulator = 0.0;
    private int _framerate = 0;
    private int _onePercentLow = 0;
    private List<string> _statProfilerEntries = [];
    private double _unaccountedCpuRenderingTime = 0;
    private List<string> _renderingProfilerEntries = [];
    
    // After profiler update, remember the number of frames the snapshot represents to calculate timing per frame
    private int _lastSeenFrameIndex = -1;

    public void RenderFrame(double deltaTime)
    {
        UpdateFramerate(deltaTime);
        RenderFramerate();
    }

    public void RenderFrameWithTiming(double delta)
    {
        var stopwatch = Profiler.Start();
        RenderFrame(delta);
        stopwatch.StopAndReport(typeof(DebugFramerateFrameRenderer), ProfilingContext.RenderingDebugFramerate);
    }

    private int _lastSeenProfilerBufferIndex = -1;
    private void RenderFramerate()
    {
        _textGrid.Draw(0, 0, DebugTextGrid.Anchor.TopRight, Color.White, "FPS: " + _framerate);
        _textGrid.Draw(0, 1, DebugTextGrid.Anchor.TopRight, Color.White, "1% Low: " + _onePercentLow);

        if (_lastSeenProfilerBufferIndex != Profiler.CurrentBufferIndex)
        {
            var framesForLatestUpdate = Math.Max(1, FrameCounter.Current - _lastSeenFrameIndex);
            _statProfilerEntries = Profiler
                .Query(ProfilingContext.BackstageUpdate |
                       ProfilingContext.PhysicsUpdate)
                .OrderByDescending(entry => entry.TotalMilliseconds / framesForLatestUpdate)
                .Select(entry =>
                {
                    var timePerFrame = entry.TotalMilliseconds / framesForLatestUpdate;
                    if (timePerFrame < 0.03)
                        return "";
                    return $"{entry.TypeName}: {timePerFrame:F2}ms";
                })
                .Where(str => str.Length > 0)
                .ToList();
            var cpuRenderingQueries = Profiler
                .Query(ProfilingContext.RenderingDebugFramerate |
                       ProfilingContext.RenderingTotal |
                       ProfilingContext.RenderingDebugLog |
                       ProfilingContext.RenderingDebugProfiler |
                       ProfilingContext.RenderingCollectAtoms |
                       ProfilingContext.RenderingCombineRequests |
                       ProfilingContext.RenderingLamina |
                       ProfilingContext.RenderingSortRequests |
                       ProfilingContext.RenderingResolveRenderTarget |
                       ProfilingContext.RenderingImmediateTextFlush |
                       ProfilingContext.RenderingPresent |
                       ProfilingContext.RenderingCullingComputerRead |
                       ProfilingContext.RenderingCullingComputerWrite |
                       ProfilingContext.RenderingFlushRegistrations |
                       ProfilingContext.RenderingSubmitAtoms);
            
            var actualCpuRenderingTime = Profiler.Query(ProfilingContext.RenderingTotal).Sum(entry => entry.TotalMilliseconds) / framesForLatestUpdate;
            var totalCpuRenderingTime = cpuRenderingQueries.Sum(entry => entry.TotalMilliseconds) / framesForLatestUpdate - actualCpuRenderingTime;
            _unaccountedCpuRenderingTime = actualCpuRenderingTime - totalCpuRenderingTime;
            _renderingProfilerEntries = cpuRenderingQueries
                .OrderByDescending(entry => entry.TotalMilliseconds / framesForLatestUpdate)
                .Select(entry =>
                {
                    var timePerFrame = entry.TotalMilliseconds / framesForLatestUpdate;
                    if (timePerFrame < 0.03)
                        return "";
                    var displayName = entry.Context.ToString()!.Replace("Rendering", "");
                    return $"{displayName}: {timePerFrame:F2}ms";
                })
                .Where(str => str.Length > 0)
                .ToList();
            
            _lastSeenFrameIndex = FrameCounter.Current;
            _lastSeenProfilerBufferIndex = Profiler.CurrentBufferIndex;
        }
        var line = 2;
        const int maxEntriesRendered = 16;
        _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.LightGreen, "---- Main Thread (CPU) ----");
        for (var i = 0; i < Math.Min(_statProfilerEntries.Count, maxEntriesRendered); i++)
        {
            var entry = _statProfilerEntries[i];
            _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.White, entry);
        }
        _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.LightGreen, "---- Render Thread (CPU) ----");
        
        for (var i = 0; i < Math.Min(_renderingProfilerEntries.Count, maxEntriesRendered); i++)
        {
            var entry = _renderingProfilerEntries[i];
            _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.White, entry);
        }
        _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.White, "Other: " + _unaccountedCpuRenderingTime.ToString("F2") + "ms");

        var drawCallCount = RenderContext.Current.ImmediateContext.GetStats().CommandCounters.DrawIndexed;
        var val = RenderContext.Current.ImmediateContext.GetStats().PrimitiveCounts;
        _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.LightGreen, "---- Stats ----");
        _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.White, $"Draws: {drawCallCount, 4}");
        _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.White, $"Instances: {RenderStats.InstancesDrawn, 4}");
        _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.White, $"Culled: {RenderStats.InstancesCulled, 4}");
        _textGrid.Draw(0, line,   DebugTextGrid.Anchor.TopRight, Color.White, $"Triangles: {val[1], 4}");
    }
    
    private void UpdateFramerate(double deltaTime)
    {
        _frameTimeAccumulator += deltaTime;
        _frameTimes.Add(deltaTime);
        if (_frameTimeAccumulator < 1.0)
            return;
        
        var averageFrameTime = _frameTimes.Count > 0 ? _frameTimes.Average() : 0.0;
        var framerate = 1.0 / averageFrameTime;

        _frameTimes.Sort();
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
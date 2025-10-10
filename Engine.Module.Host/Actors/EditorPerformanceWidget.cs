using System.Collections.Immutable;
using System.Drawing;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;
using Engine.Core.Profiling;

namespace Engine.Module.Host.Actors;

public partial class EditorPerformanceWidget : Actor
{
    [Component] protected FramerateCounterComponent FramerateCounter;
    [Component] protected UserInterfaceComponent FramerateLabelWidget;
    [Component] protected UserInterfaceComponent FramerateGraphWidget;
    
    // [OnTimer(Frames = 60)]
    // protected void OnTimer()
    // {
    //     FramerateLabelWidget.SetLayout(v => 
    //     {
    //         v.Div(new Vector2(100, 300), v =>
    //         {
    //             v.Button(label: "Test Button", backgroundColor: Color.Aqua);
    //         });
    //     });
    // }

    [OnTimer(Seconds = 0.5)]
    protected void OnGraphUpdate()
    {
        FramerateGraphWidget.SetLayout(v =>
        {
            v.Div(position: (1300, 24), v =>
            {
                var scaleFactor = 800.0 / FramerateCounter.MaximumValue;
                var graphData = FramerateCounter.FramerateHistory
                    .Select((val, index) => new Vector2(index * 25, -val * scaleFactor))
                    .ToImmutableList();
                v.Line(points: graphData);
                var lowGraphData = FramerateCounter.LowFramerateHistory
                    .Select((val, index) => new Vector2(index * 25, -val * scaleFactor))
                    .ToImmutableList();
                v.Line(points: lowGraphData, color: Color.Red);
            });
        });
    }
}

public partial class FramerateCounterComponent : ActorComponent
{
    internal readonly List<double> FramerateHistory = Enumerable.Repeat(0.0, 256).ToList();
    internal readonly List<double> LowFramerateHistory = Enumerable.Repeat(0.0, 256).ToList();
    internal int MaximumValue = 240;

    private readonly List<double> _frameTimes = [];
    private double _frameTimeAccumulator = 0.0;
    private int _framerate = 0;
    private int _onePercentLow = 0;
    private List<string> _statProfilerEntries = [];
    private List<string> _renderingProfilerEntries = [];
    
    // After profiler update, remember the number of frames the snapshot represents to calculate timing per frame
    private int _lastSeenFrameIndex = -1;
    private int _updateFramesCollected = 0;

    [OnUpdate]
    protected void CollectFrameTimes(double deltaTime)
    {
        _frameTimeAccumulator += deltaTime;
        _frameTimes.Add(deltaTime);
        _updateFramesCollected++;
    }
    
    [OnTimer(Seconds = 0.1)]
    protected void FlushFrameTimes()
    {
        // _textGrid.Draw(0, 0, DebugTextGrid.Anchor.TopRight, Color.White, "FPS: " + _framerate);
        // _textGrid.Draw(0, 1, DebugTextGrid.Anchor.TopRight, Color.White, "1% Low: " + _onePercentLow);
        
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
        
        FramerateHistory.Add(_framerate);
        LowFramerateHistory.Add(_onePercentLow);
        FramerateHistory.RemoveAt(0);
        LowFramerateHistory.RemoveAt(0);
        MaximumValue = (int)Math.Max(FramerateHistory.Max(), LowFramerateHistory.Max());

        // var framesForLatestUpdate = Math.Max(1, FrameCounter.Current - _lastSeenFrameIndex);
        var framesForLatestUpdate = Math.Max(1, _updateFramesCollected);
        _statProfilerEntries = Profiler
            .Query(ProfilingContext.BackstageUpdate |
                   ProfilingContext.PhysicsUpdate |
                   ProfilingContext.RenderingTotal)
            .OrderByDescending(entry => entry.TotalMilliseconds / framesForLatestUpdate)
            .Select(entry =>
            {
                var timePerFrame = entry.TotalMilliseconds / framesForLatestUpdate;
                if (timePerFrame < 0.01)
                    return "";
                return $"{entry.TypeName}: {timePerFrame:F2}ms";
            })
            .Where(str => str.Length > 0)
            .ToList();
        _renderingProfilerEntries = Profiler
            .Query(ProfilingContext.RenderingDebugFramerate |
                   ProfilingContext.RenderingDebugLog |
                   ProfilingContext.RenderingDebugProfiler |
                   ProfilingContext.RenderingCollectAtoms |
                   ProfilingContext.RenderingCombineRequests |
                   ProfilingContext.RenderingLamina |
                   ProfilingContext.RenderingSortRequests |
                   ProfilingContext.RenderingPresent |
                   ProfilingContext.RenderingFlushRegistrations |
                   ProfilingContext.RenderingSubmitAtoms)
            .OrderByDescending(entry => entry.TotalMilliseconds / framesForLatestUpdate)
            .Select(entry =>
            {
                var timePerFrame = entry.TotalMilliseconds / framesForLatestUpdate;
                if (timePerFrame < 0.01)
                    return "";
                var displayName = entry.Context.ToString()!.Replace("Rendering", "");
                return $"{displayName}: {timePerFrame:F2}ms";
            })
            .Where(str => str.Length > 0)
            .ToList();
        
        _lastSeenFrameIndex = FrameCounter.Current;
        _updateFramesCollected = 0;
        
        // _lastSeenProfilerBufferIndex = Profiler.CurrentBufferIndex;
        // var line = 2;
        // const int maxEntriesRendered = 16;
        // for (var i = 0; i < Math.Min(_statProfilerEntries.Count, maxEntriesRendered); i++)
        // {
        //     var entry = _statProfilerEntries[i];
        //     _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.White, entry);
        // }
        // _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.LightGreen, "---- Rendering ----");
        // for (var i = 0; i < Math.Min(_renderingProfilerEntries.Count, maxEntriesRendered); i++)
        // {
        //     var entry = _renderingProfilerEntries[i];
        //     _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.White, entry);
        // }
        //
        // var drawCallCount = RenderContext.Current.ImmediateContext.GetStats().CommandCounters.DrawIndexed;
        // var val = RenderContext.Current.ImmediateContext.GetStats().PrimitiveCounts;
        // _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.LightGreen, "---- Stats ----");
        // _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.White, $"Draws: {drawCallCount, 4}");
        // _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.White, $"Instances: {RenderStats.InstancesDrawn, 4}");
        // _textGrid.Draw(0, line++, DebugTextGrid.Anchor.TopRight, Color.White, $"Culled: {RenderStats.InstancesCulled, 4}");
        // _textGrid.Draw(0, line,   DebugTextGrid.Anchor.TopRight, Color.White, $"Triangles: {val[1], 4}");
    }
}
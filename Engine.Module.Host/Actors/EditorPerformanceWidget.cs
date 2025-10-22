using System.Collections.Immutable;
using System.Drawing;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Lamina;
using Engine.Core.Profiling;
using Engine.Module.Host.Utilities;

namespace Engine.Module.Host.Actors;

public partial class EditorPerformanceWidget : Actor
{
    [Component] protected FramerateCounterComponent FramerateCounter;
    [Component] protected UserInterfaceComponent FramerateLabelWidget;
    [Component] protected UserInterfaceComponent FramerateGraphWidget;
    [Component] protected UserInterfaceComponent PerformanceMetricsWidget;

    [Component] protected LaminaDebugTextGrid TextGrid;

    [OnTimer(Seconds = 0.1)]
    protected void OnGraphUpdate()
    {
        FramerateLabelWidget.SetLayout(v =>
        {
            FramerateLabelWidget.Size = (512, Backstage.Window.FramebufferSize.Y);
            FramerateLabelWidget.Transform.Position = (Backstage.Window.FramebufferSize.X - 512, 0, 0);
            
            v.Div(position: (0, 0), children: v =>  
            {
                FramerateCounter.WriteFramerateToGrid(TextGrid, v, FramerateLabelWidget.Size);
            }); 
        });
        
        FramerateGraphWidget.SetLayout(v => 
        {
            FramerateGraphWidget.Padding = (8, 4);
            FramerateGraphWidget.Size = (FramerateCounterComponent.ValuesStored + FramerateGraphWidget.Padding.X * 2, 48);
            FramerateGraphWidget.Transform.Position = new Vector3(Backstage.Window.FramebufferSize.X - 300, 8, 0);
            FramerateGraphWidget.BackgroundColor = Color.FromArgb(50, 0, 0, 0);
            
            v.Div(position: (0, 0), children: v =>
            {
                var height = FramerateGraphWidget.ContentSize.Y;
                var scaleFactor = height / FramerateCounter.MaximumValue;
                var graphData = FramerateCounter.FramerateHistory
                    .Select((val, index) => new Vector2(index, height - val * scaleFactor))
                    .ToImmutableList();
                v.Line(points: graphData);
                var lowGraphData = FramerateCounter.LowFramerateHistory
                    .Select((val, index) => new Vector2(index, height - val * scaleFactor))
                    .ToImmutableList();
                v.Line(points: lowGraphData, color: Color.Red);
            });
        });
    }
    
    [OnTimer(Seconds = 1.0)]
    protected void OnMetricsUpdate()
    {
        PerformanceMetricsWidget.SetLayout(v =>
        {
            PerformanceMetricsWidget.Size = (512, Backstage.Window.FramebufferSize.Y);
            PerformanceMetricsWidget.Transform.Position = (Backstage.Window.FramebufferSize.X - 512, 0, 0);
            FramerateCounter.WriteMetricsToGrid(TextGrid, v, PerformanceMetricsWidget.ContentSize);
        });
    }
}

public partial class FramerateCounterComponent : ActorComponent
{
    internal const int ValuesStored = 128;
    internal readonly List<double> FramerateHistory = Enumerable.Repeat(0.0, ValuesStored).ToList();
    internal readonly List<double> LowFramerateHistory = Enumerable.Repeat(0.0, ValuesStored).ToList();
    internal int MaximumValue = 240;

    private readonly List<double> _frameTimes = [];
    private double _frameTimeAccumulator = 0.0;
    private int _framerate = 0;
    private int _onePercentLow = 0;
    private List<string> _statProfilerEntries = [];
    private double _unaccountedCpuRenderingTime = 0;
    private List<string> _renderingProfilerEntries = [];
    
    private int _updateFramesCollected = 0;

    [OnUpdate]
    protected void CollectFrameTimes(double deltaTime)
    {
        _frameTimeAccumulator += deltaTime;
        _frameTimes.Add(deltaTime);
        _updateFramesCollected++;
    }
    
    [OnTimer(Seconds = 0.5)]
    protected void CollectFrameTimes()
    {
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
    }
    
    [OnTimer(Seconds = 1.0)]
    protected void CollectPerformanceMetrics()
    {
        // var framesForLatestUpdate = Math.Max(1, FrameCounter.Current - _lastSeenFrameIndex);
        var framesForLatestUpdate = Math.Max(1, _updateFramesCollected);
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
        
        _updateFramesCollected = 0;
    }
    
    public void WriteFramerateToGrid(LaminaDebugTextGrid textGrid, LaminaLayout v, Vector2 canvasSize)
    {
        textGrid.Draw(v, 0, 0, LaminaDebugTextGrid.Anchor.TopRight, Color.White, "FPS: " + _framerate, canvasSize);
        textGrid.Draw(v, 0, 1, LaminaDebugTextGrid.Anchor.TopRight, Color.White, "1% Low: " + _onePercentLow, canvasSize);
    }

    public void WriteMetricsToGrid(LaminaDebugTextGrid textGrid, LaminaLayout v, Vector2 canvasSize)
    {
        var line = 2;
        const int maxEntriesRendered = 16;
        textGrid.Draw(v, 0, line++, LaminaDebugTextGrid.Anchor.TopRight, Color.LightGreen, "---- Main Thread (CPU) ----", canvasSize);
        for (var i = 0; i < Math.Min(_statProfilerEntries.Count, maxEntriesRendered); i++)
        {
            var entry = _statProfilerEntries[i];
            textGrid.Draw(v, 0, line++, LaminaDebugTextGrid.Anchor.TopRight, Color.White, entry, canvasSize);
        }
        textGrid.Draw(v, 0, line++, LaminaDebugTextGrid.Anchor.TopRight, Color.LightGreen, "---- Render Thread (CPU) ----", canvasSize);
        
        for (var i = 0; i < Math.Min(_renderingProfilerEntries.Count, maxEntriesRendered); i++)
        {
            var entry = _renderingProfilerEntries[i];
            textGrid.Draw(v, 0, line++, LaminaDebugTextGrid.Anchor.TopRight, Color.White, entry, canvasSize);
        }
        if (_unaccountedCpuRenderingTime > 0.02)
            textGrid.Draw(v, 0, line++, LaminaDebugTextGrid.Anchor.TopRight, Color.White, "Other: " + _unaccountedCpuRenderingTime.ToString("F2") + "ms", canvasSize);
        
        var drawCallCount = RenderContext.Current.ImmediateContext.GetStats().CommandCounters.DrawIndexed;
        var val = RenderContext.Current.ImmediateContext.GetStats().PrimitiveCounts;
        var renderStats = RenderStats.GetPreviousFrameStats();
        textGrid.Draw(v, 0, line++, LaminaDebugTextGrid.Anchor.TopRight, Color.LightGreen, "---- Stats ----", canvasSize);
        textGrid.Draw(v, 0, line++, LaminaDebugTextGrid.Anchor.TopRight, Color.White, $"Draws: {drawCallCount, 4}", canvasSize);
        textGrid.Draw(v, 0, line++, LaminaDebugTextGrid.Anchor.TopRight, Color.White, $"Instances: {renderStats.NumInstancesDrawn, 4}", canvasSize);
        textGrid.Draw(v, 0, line++, LaminaDebugTextGrid.Anchor.TopRight, Color.White, $"Culled: {renderStats.NumInstancesCulled, 4}", canvasSize);
        textGrid.Draw(v, 0, line,   LaminaDebugTextGrid.Anchor.TopRight, Color.White, $"Triangles: {val[1], 4}", canvasSize);
    }
}
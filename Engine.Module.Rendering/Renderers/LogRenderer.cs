using System.Drawing;
using Engine.Core.Common;
using Engine.Core.Logging;
using Engine.Core.Profiling;
using Engine.Module.Rendering.Abstract;

namespace Engine.Module.Rendering.Renderers;

public enum LoggingMode
{
    None,
    Recent,
    Info,
    Warn,
    Error,
    Bgfx,
    Count,
}

public class LogRenderer(RenderingModule parent): Renderer(parent)
{
    private LoggingMode _mode = LoggingMode.Recent;
    private readonly List<double> _frameTimes = [];
    private double _frameTimeAccumulator = 0.0;
    private int _framerate = 0;
    private int _onePercentLow = 0;
    // private double[] _backstageFrametime;

    public void OnToggleMode()
    {
        _mode += 1;
        if (_mode > LoggingMode.Count - 1)
            _mode = LoggingMode.None;
        
        // if (_mode is LoggingMode.Bgfx or LoggingMode.None)
            // Module.ToggleDebugFlags(DebugFlags.Stats | DebugFlags.Profiler);
    }

    protected internal override void RenderFrame(double deltaTime)
    {
        UpdateFramerate(deltaTime);
        
        RenderLogging();
        RenderFramerate();
    }

    private void RenderLogging()
    {
        if (_mode is not LoggingMode.None)
        {
            // DebugTextWrite(0, 0, DebugColor.Black, DebugColor.DarkGray, _mode.ToString());
        }

        var messageCount = 0;
        var persistentMessages = Logger.ReadPersistent();
        if (persistentMessages == null)
            throw new InvalidOperationException("Failed to read persistent messages from logger.");
        foreach (var (message, level) in persistentMessages)
        {
            messageCount += 1;
            // DebugTextWrite(0, messageCount, DebugColor.Black, GetLogColor(level), message);
        }

        if (_mode is LoggingMode.None or LoggingMode.Bgfx)
            return;

        List<Tuple<string, LogLevel>> messages = [];
        switch (_mode)
        {
            case LoggingMode.Recent:
                Logger.ReadRecent(out messages);
                break;
            case LoggingMode.Info:
                Logger.ReadLevel(LogLevel.Info, out messages);
                break;
            case LoggingMode.Warn:
                Logger.ReadLevel(LogLevel.Warn, out messages);
                break;
            case LoggingMode.Error:
                Logger.ReadLevel(LogLevel.Error, out messages);
                break;
            case LoggingMode.None:
            case LoggingMode.Bgfx:
            case LoggingMode.Count:
            default:
                break;
        }
        foreach (var message in messages)
        {
            messageCount += 1;
            RenderLogEntry(message.Item1, message.Item2, messageCount);
        }
    }

    private static void RenderLogEntry(string message, LogLevel level, int messageCount)
    {
        // DebugTextWrite(0, messageCount, DebugColor.Black, GetLogColor(level), message);
    }
    
    private void RenderFramerate()
    {
        parent._fontRenderer.RenderText("FPS: " + _framerate, new Vector2(1000, 0), Color.LightGray);
        // DebugTextWrite(Module.FramebufferSize.X / 8 - 9, 0, "FPS: " + _framerate);
        // DebugTextWrite(Module.FramebufferSize.X / 8 - 12, 1, "1%% Low: " + _onePercentLow);

        var line = 2;
        var updates = Profiler.Query(ProfilingContext.BackstageUpdate | ProfilingContext.PhysicsUpdate | ProfilingContext.RenderingPrepare);
        foreach (var entry in updates)
        {
            var length = 9 + entry.TypeName.Length;
            // DebugTextWrite(Module.FramebufferSize.X / 8 - length, line++, $"{entry.TypeName}: {entry.AverageMilliseconds():F2}ms");
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

    // private static DebugColor GetLogColor(LogLevel level)
    // {
        // return level switch
        // {
            // LogLevel.Debug => DebugColor.LightGray,
            // LogLevel.Info => DebugColor.LightGreen,
            // LogLevel.Warn => DebugColor.Yellow,
            // LogLevel.Error => DebugColor.Red,
            // LogLevel.Fatal => DebugColor.Red,
            // LogLevel.Log => DebugColor.LightCyan,
            // _ => DebugColor.White
        // };
    // }
}
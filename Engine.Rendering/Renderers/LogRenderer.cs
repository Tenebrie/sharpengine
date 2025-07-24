using Engine.Core.Logging;
using Engine.Rendering.Abstract;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;

namespace Engine.Rendering.Renderers;

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

public class LogRenderer(RenderingCore parent): Renderer(parent)
{
    private LoggingMode _mode = LoggingMode.Recent;
    private readonly List<double> _frameTimes = [];
    private int _framerate = 0;
    private int _onePercentLow = 0;

    public void OnToggleMode()
    {
        _mode += 1;
        if (_mode > LoggingMode.Count - 1)
            _mode = LoggingMode.None;
        
        if (_mode is LoggingMode.Bgfx or LoggingMode.None)
            Core.ToggleDebugFlags(DebugFlags.Stats | DebugFlags.Profiler);
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
            DebugTextWrite(0, 0, DebugColor.Black, DebugColor.DarkGray, _mode.ToString());
        }

        var messageCount = 0;
        Logger.ReadPersistent(out var persistentMessages);
        foreach (var message in persistentMessages)
        {
            messageCount += 1;
            DebugTextWrite(0, messageCount, DebugColor.Black, DebugColor.LightRed, message);
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

    private static void RenderLogEntry(string message, LogLevel logLevel, int messageCount)
    {
        var color = logLevel switch
        {
            LogLevel.Debug => DebugColor.LightGray,
            LogLevel.Info => DebugColor.LightGreen,
            LogLevel.Warn => DebugColor.Yellow,
            LogLevel.Error => DebugColor.Red,
            LogLevel.Fatal => DebugColor.Red,
            LogLevel.Log => DebugColor.LightCyan,
            _ => DebugColor.White
        };
        DebugTextWrite(0, messageCount, DebugColor.Black, color, message);
    }
    
    private void RenderFramerate()
    {
        DebugTextWrite(Core.FramebufferSize.X / 8 - 9, 0, "FPS: " + _framerate);
        DebugTextWrite(Core.FramebufferSize.X / 8 - 12, 1, "1%% Low: " + _onePercentLow);
    }
 
    private void UpdateFramerate(double deltaTime)
    {
        _frameTimes.Add(deltaTime);
        if (_frameTimes.Count < 100)
            return;
        
        var averageFrameTime = _frameTimes.Count > 0 ? _frameTimes.Average() : 0.0;
        var framerate = 1.0f / averageFrameTime;
        
        var onePercentCount = Math.Max(1, (int)Math.Ceiling(_frameTimes.Count * 0.01));
        var slowestFrames = _frameTimes.OrderByDescending(x => x).Take(onePercentCount).ToList();
        var onePercentLowFrameTime = slowestFrames.Count != 0 ? slowestFrames.Average() : 0.0;
        var onePercentLow = onePercentLowFrameTime > 0 ? 1.0 / onePercentLowFrameTime : 0.0;
        
        _framerate = (int)Math.Round(framerate);
        _onePercentLow = (int)Math.Round(onePercentLow);
        _frameTimes.Clear();
    }
}
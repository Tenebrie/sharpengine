using System.Drawing;
using Engine.Core.Logging;
using Engine.Core.Profiling;
using Engine.Module.Rendering.Utilities;

namespace Engine.Module.Rendering.Renderers.Debug;

public enum LoggingMode
{
    None,
    Recent,
    Info,
    Warn,
    Error,
    Count,
}

public class DebugLogRenderer(RenderingHost host): IRenderer
{
    private readonly DebugTextGrid _textGrid = new(host);
    
    private LoggingMode _mode = LoggingMode.Recent;

    public void OnToggleMode()
    {
        _mode += 1;
        if (_mode > LoggingMode.Count - 1)
            _mode = LoggingMode.None;
    }

    public void RenderFrameWithTiming(double delta)
    {
        var stopwatch = Profiler.Start();
        RenderFrame(delta);
        stopwatch.StopAndReport(typeof(DebugProfilerRenderer), ProfilingContext.RenderingDebugLog);
    }

    public void RenderFrame(double deltaTime)
    {
        if (_mode is not LoggingMode.None)
        {
            _textGrid.Draw(0, 0, DebugTextGrid.Anchor.TopLeft, Color.DarkGray, _mode.ToString());
        }

        var messageCount = 0;
        var persistentMessages = Logger.ReadPersistent();
        if (persistentMessages == null)
            throw new InvalidOperationException("Failed to read persistent messages from logger.");
        foreach (var (message, level) in persistentMessages)
        {
            messageCount += 1;
            _textGrid.Draw(0, messageCount, DebugTextGrid.Anchor.TopLeft, level.GetColor(), message);
        }

        if (_mode is LoggingMode.None)
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
            case LoggingMode.Count:
            default:
                break;
        }
        foreach (var message in messages.Take(40))
        {
            messageCount += 1;
            RenderLogEntry(message.Item1, message.Item2, messageCount);
        }
    }

    private void RenderLogEntry(string message, LogLevel level, int messageCount)
    {
        _textGrid.Draw(0, messageCount, DebugTextGrid.Anchor.TopLeft, level.GetColor(), message);
    }
}
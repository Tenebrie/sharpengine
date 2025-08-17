using System.Drawing;
using Engine.Core.Common;
using Engine.Core.Logging;
using Engine.Core.Profiling;
using Engine.Core.Profiling.Attributes;
using Engine.Module.Rendering.Abstract;

namespace Engine.Module.Rendering.Renderers;

public enum LoggingMode
{
    None,
    Recent,
    Info,
    Warn,
    Error,
    Count,
}

public class LogRenderer(RenderingModule parent): Renderer(parent)
{
    private LoggingMode _mode = LoggingMode.Recent;
    private readonly List<double> _frameTimes = [];
    private double _frameTimeAccumulator = 0.0;
    private int _framerate = 0;
    private int _onePercentLow = 0;

    private const float RenderScale = 1.5f;
    private const int FontSize = (int)(18.0 * RenderScale);
    private const double Padding = 2.0 * RenderScale;
    private const double LineHeight = 1.0 + 0.2 * RenderScale;

    public void OnToggleMode()
    {
        _mode += 1;
        if (_mode > LoggingMode.Count - 1)
            _mode = LoggingMode.None;
    }

    [Profile]
    protected internal override void RenderFrame(double deltaTime)
    {
        UpdateFramerate(deltaTime);
        
        RenderLogging();
        RenderFramerate();
        RenderProfiler();
    }

    [Profile]
    private void RenderLogging()
    {
        if (_mode is not LoggingMode.None)
        {
            DrawLogText(0, 0, Anchor.TopLeft, Color.DarkGray, _mode.ToString());
        }

        var messageCount = 0;
        var persistentMessages = Logger.ReadPersistent();
        if (persistentMessages == null)
            throw new InvalidOperationException("Failed to read persistent messages from logger.");
        foreach (var (message, level) in persistentMessages)
        {
            messageCount += 1;
            DrawLogText(0, messageCount, Anchor.TopLeft, GetLogColor(level), message);
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
        DrawLogText(0, messageCount, Anchor.TopLeft, GetLogColor(level), message);
    }
    
    [Profile]
    private void RenderFramerate()
    {
        DrawLogText(0, 0, Anchor.TopRight, Color.White, "FPS: " + _framerate);
        DrawLogText(0, 1, Anchor.TopRight, Color.White, "1% Low: " + _onePercentLow);

        var line = 2;
        var updates = Profiler.Query(ProfilingContext.BackstageUpdate | ProfilingContext.PhysicsUpdate | ProfilingContext.RenderingPrepare);
        foreach (var entry in updates.Take(32))
        {
            DrawLogText(0, line++, Anchor.TopRight, Color.White, $"{entry.TypeName}: {entry.AverageMilliseconds():F2}ms");
        }
    }
    
    [Profile]
    private void RenderProfiler()
    {
        var entries = Profiler.QueryWorstOffenders().Take(10).ToList();
        var line = 2;
        foreach (var entry in entries)
        {
            DrawLogText(50, line++, Anchor.TopLeft, Color.White, $"{entry.FullName}: {entry.AverageMilliseconds():F2}ms");
        }
    }
    
    private bool _textMeasured = false;
    private int _glyphWidth = 0;
    private int _glyphHeight = 0;
    private enum Anchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
    private void DrawLogText(int x, int y, Anchor anchor, Color color, string text)
    {
        if (text.Length > 512)
            text = string.Concat(text.AsSpan(0, 512), "...");
        if (!_textMeasured)
        {
            var singlyGlyphSize = Module.TextRenderer.MeasureText("RobotoMono-Bold", FontSize, "0");
            _glyphWidth = (int)singlyGlyphSize.X;
            _glyphHeight = (int)singlyGlyphSize.Y;
            _textMeasured = true;
        }

        var size = Module.TextRenderer.MeasureText("RobotoMono-Bold", FontSize, text).X;
        var offset = new Vector2(x * _glyphWidth, y * _glyphHeight * LineHeight);
        var position = anchor switch
        {
            Anchor.TopLeft => new Vector2(offset.X + Padding, offset.Y),
            Anchor.TopRight => new Vector2(Module.RootWindow.FramebufferSize.X - size - offset.X - Padding, offset.Y),
            Anchor.BottomLeft => new Vector2(offset.X + Padding, Module.RootWindow.FramebufferSize.Y - _glyphHeight - offset.Y - Padding),
            _ => new Vector2(
                Module.RootWindow.FramebufferSize.X - size - offset.X - Padding,
                Module.RootWindow.FramebufferSize.Y - _glyphHeight - offset.Y - Padding
            )
        };
        Module.TextRenderer.RenderText("RobotoMono-Bold", FontSize, text, position, color, 2);
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

    private static Color GetLogColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => Color.LightGray,
            LogLevel.Info => Color.LightGreen,
            LogLevel.Warn => Color.Yellow,
            LogLevel.Error => Color.Red,
            LogLevel.Fatal => Color.Red,
            LogLevel.Log => Color.LightCyan,
            _ => Color.White
        };
    }
}
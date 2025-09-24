using System.Drawing;
using Engine.Core.Common;
using Engine.Core.Logging;

namespace Engine.Module.Rendering.Utilities;

public class DebugTextGrid(RenderingHost host)
{
    public enum Anchor
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
    
    private const float RenderScale = 1.5f;
    private const int FontSize = (int)(18.0 * RenderScale);
    private const double Padding = 2.0 * RenderScale;
    private const double LineHeight = 1.0 + 0.2 * RenderScale;

    private bool _textMeasured = false;
    private int _glyphWidth = 0;
    private int _glyphHeight = 0;

    public void Draw(int x, int y, Anchor anchor, Color color, string text)
    {
        if (text.Length > 512)
            text = string.Concat(text.AsSpan(0, 512), "...");
        if (!_textMeasured)
        {
            var singleGlyphSize = host.ImmediateTextRenderer.MeasureText("RobotoMono-Bold", FontSize, "0");
            _glyphWidth = (int)singleGlyphSize.X;
            _glyphHeight = (int)singleGlyphSize.Y;
            _textMeasured = true;
        }

        var size = host.ImmediateTextRenderer.MeasureText("RobotoMono-Bold", FontSize, text).X;
        var offset = new Vector2(x * _glyphWidth, y * _glyphHeight * LineHeight);
        var position = anchor switch
        {
            Anchor.TopLeft => new Vector2(offset.X + Padding, offset.Y),
            Anchor.TopRight => new Vector2(host.RootWindow.FramebufferSize.X - size - offset.X - Padding, offset.Y),
            Anchor.BottomLeft => new Vector2(offset.X + Padding, host.RootWindow.FramebufferSize.Y - _glyphHeight - offset.Y - Padding),
            Anchor.BottomRight => new Vector2(
                host.RootWindow.FramebufferSize.X - size - offset.X - Padding,
                host.RootWindow.FramebufferSize.Y - _glyphHeight - offset.Y - Padding
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null)
        };
        host.ImmediateTextRenderer.RenderText("RobotoMono-Bold", FontSize, text, position, color, 2);
    }
}
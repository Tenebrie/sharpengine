using System.Drawing;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Lamina;
using Engine.Core.Logging;

namespace Engine.Module.Host.Utilities;

public partial class LaminaDebugTextGrid : ActorComponent
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

    public void Draw(LaminaLayout v, int x, int y, Anchor anchor, Color color, string text)
    {
        // var textRenderer = RenderContext.Current
        if (text.Length > 512)
            text = string.Concat(text.AsSpan(0, 512), "...");
        if (!_textMeasured)
        {
            // TODO: Fix measuring text (needs access to the text renderer)
            var singleGlyphSize = new Vector2(12.5, 23);
            // var singleGlyphSize = textRenderer.MeasureText("RobotoMono-Bold", FontSize, "0");
            _glyphWidth = (int)singleGlyphSize.X;
            _glyphHeight = (int)singleGlyphSize.Y;
            _textMeasured = true;
        }

        var size = 12.5 * text.Length;
        // var size = textRenderer.MeasureText("RobotoMono-Bold", FontSize, text).X;
        var offset = new Vector2(x * _glyphWidth, y * _glyphHeight * LineHeight);
        var position = anchor switch
        {
            Anchor.TopLeft => new Vector2(offset.X + Padding, offset.Y),
            Anchor.TopRight => new Vector2(Backstage.Window.FramebufferSize.X - size - offset.X - Padding, offset.Y),
            Anchor.BottomLeft => new Vector2(offset.X + Padding, Backstage.Window.FramebufferSize.Y - _glyphHeight - offset.Y - Padding),
            Anchor.BottomRight => new Vector2(
                Backstage.Window.FramebufferSize.X - size - offset.X - Padding,
                Backstage.Window.FramebufferSize.Y - _glyphHeight - offset.Y - Padding
            ),
            _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, null)
        };
        v.Label(text: text, position: position, color: color, font: "RobotoMono-Bold", fontSize: FontSize);
        // v.Label("RobotoMono-Bold", FontSize, text, position, color, 2);
    }
}
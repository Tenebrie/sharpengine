using System.Diagnostics.CodeAnalysis;
using Engine.Core.Common;
using Engine.Core.Logging;
using Engine.Core.Profiling.Attributes;
using Color = System.Drawing.Color;

namespace Engine.Module.Rendering.Fonts;

public struct FontKey : IEquatable<FontKey>
{
    public required string Name;
    public required int Size;
    public required int SampleCount;

    public override int GetHashCode() => Name.GetHashCode() ^ Size.GetHashCode();
    public bool Equals(FontKey other) => GetHashCode() == other.GetHashCode() && Name == other.Name && Size == other.Size;
    public override bool Equals([NotNullWhen(true)] object? obj) => base.Equals(obj);

    public static bool operator ==(FontKey left, FontKey right) => left.Equals(right);
    public static bool operator !=(FontKey left, FontKey right) => !(left == right);
}

public class TextRenderer : IDisposable
{
    private readonly Dictionary<FontKey, FontRenderer> _fonts = new();

    private FontRenderer? ProduceRenderer(string font, int size)
    {
        var key = new FontKey
        {
            Name = font,
            Size = size,
            SampleCount = 2,
        };

        if (_fonts.TryGetValue(key, out var fontRenderer))
            return fontRenderer;
        
        try
        {
            _fonts[key] = fontRenderer = new FontRenderer(key);
            fontRenderer.Initialize();
        }
        catch (Exception e)
        {
            Logger.Error($"Failed to initialize font renderer for {key.Name} at size {key.Size}: {e.Message}");
            Console.Error.WriteLine(e);
            return null;
        }

        return fontRenderer;
    }
    
    public void RenderText(string font, int size, string text, Vector2 position, Color color, int shadowBlur = 0)
    {
        var fontRenderer = ProduceRenderer(font, size);
        fontRenderer?.RenderText(text, position, color, shadowBlur);
    }
    
    public Vector2 MeasureText(string font, int size, string text)
    {
        var fontRenderer = ProduceRenderer(font, size);
        return fontRenderer?.MeasureString(text) ?? Vector2.Zero;
    }

    public void Flush()
    {
        foreach (var font in _fonts.Values)
            font.Flush();
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        foreach (var font in _fonts.Values)
            font.Dispose();
        _fonts.Clear();
    }
}

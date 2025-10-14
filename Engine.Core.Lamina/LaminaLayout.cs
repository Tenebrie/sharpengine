using System.Drawing;
using Engine.Core.Common;
using Engine.Core.Modules;
using Box = Engine.Core.Common.Box;

namespace Engine.Core.Lamina;

public partial record LaminaLayout(Type Type) : ILaminaLayout
{
    public readonly List<LaminaLayout> Children = [];

    public virtual bool Equals(LaminaLayout? other)
        => other is not null
           && Type == other.Type
           && Children.SequenceEqual(other.Children);

    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.Add(Type);
        foreach (var child in Children)
            hc.Add(child);
        return hc.ToHashCode();
    }

    public void Add(LaminaLayout w)
    {
        Children.Add(w);
    }
    public void Add(LaminaLayout w, Action<LaminaLayout> render)
    {
        Children.Add(w);
        render.Invoke(w);
    }
}

/// <summary>
/// Button
/// </summary>
public record ButtonLayout(LaminaButtonProps Props) : LaminaLayout(typeof(ButtonLayout));
public record struct LaminaButtonProps()
{
    public string Label = "";
    public Color BackgroundColor = Color.Gray;
    public Action? OnClick;
}

/// <summary>
/// Div
/// </summary>
public record DivLayout(LaminaDivProps Props) : LaminaLayout(typeof(DivLayout));
public record struct LaminaDivProps()
{
    public Vector2 Position = Vector2.Zero;
    public Action<LaminaLayout> Children;
}

/// <summary>
/// Image
/// </summary>
public record ImageLayout(LaminaImageProps Props) : LaminaLayout(typeof(ImageLayout));
public record struct LaminaImageProps()
{
    public string? ImagePath = null;
    public Box ClippingRect = Box.Full;
    public Vector2 Position = Vector2.Zero;
    public Vector2 Size = new Vector2(100, 100);
    public Color Tint = Color.White;
}

/// <summary>
/// Label
/// </summary>
public record LabelLayout(LaminaLabelProps Props) : LaminaLayout(typeof(LabelLayout));
public record struct LaminaLabelProps()
{
    public string Text;
    public string Font = "RobotoMono-Bold";
    public int FontSize = 18;
    public Color Color = Color.Black;
    public Vector2 Position = Vector2.Zero;
}

/// <summary>  
/// Line
/// </summary>
public record LineLayout(LaminaLineProps Props) : LaminaLayout(typeof(LineLayout));
public record struct LaminaLineProps()
{
    public IReadOnlyList<Vector2> Points;
    public Color Color = Color.Black;
    public int Thickness = 1;
}

public record SpacerLayout(SpacerProps Props) : LaminaLayout(typeof(SpacerLayout));
public record struct SpacerProps()
{
    public Vector2 Size = new(10, 10);
}

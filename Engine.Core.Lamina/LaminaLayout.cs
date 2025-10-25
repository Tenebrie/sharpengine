using System.Drawing;
using Engine.Core.Common;
using Engine.Core.Modules;
using Box = Engine.Core.Common.Box;

namespace Engine.Core.Lamina;

public interface ILaminaProps
{
    public Vector2 Position { get; set; }
    public Vector2 Size { get; set; }
    public Vector2 Padding { get; set; }
    public LaminaFillMode FillMode { get; set; }
}

public enum LaminaFillMode
{
    None,
    FillContainer,
    FillHorizontal,
    FillVertical
}

public partial record LaminaLayout(Type Type, ILaminaProps SharedProps) : ILaminaLayout
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
public record ButtonLayout(LaminaButtonProps Props) : LaminaLayout(typeof(ButtonLayout), Props);
public partial record struct LaminaButtonProps() : ILaminaProps
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public Vector2 Size { get; set; } = Vector2.Zero;
    public Vector2 Padding { get; set; } = Vector2.Zero;
    public LaminaFillMode FillMode { get; set; } = LaminaFillMode.None;
    public ILaminaRenderContext RenderContext { get; set; } = null!;
    
    public string Label = "";
    public Color BackgroundColor = Color.Gray;
    public Action? OnClick;
    public Vector4Shorthand BorderRadius = 0;
}

/// <summary>
/// Div
/// </summary>
public record BoxLayout(LaminaBoxProps Props) : LaminaLayout(typeof(BoxLayout), Props);
public partial record struct LaminaBoxProps() : ILaminaProps
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public Vector2 Size { get; set; } = -Vector2.One;
    public Vector2 Padding { get; set; } = Vector2.Zero;
    public LaminaFillMode FillMode { get; set; } = LaminaFillMode.None;
    public Action<LaminaLayout> Children;
}

/// <summary>
/// Flex
/// </summary>
public record FlexLayout(LaminaFlexProps Props) : LaminaLayout(typeof(FlexLayout), Props);
public partial record struct LaminaFlexProps() : ILaminaProps
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public Vector2 Size { get; set; } = -Vector2.One;
    public Vector2 Padding { get; set; } = Vector2.Zero;
    public LaminaFillMode FillMode { get; set; } = LaminaFillMode.None;
    
    public double Gap = 0;
    public LaminaFlexAlign AlignContent = LaminaFlexAlign.Center;
    public LaminaFlexAlign JustifyContent = LaminaFlexAlign.Center;
    public LaminaFlexDirection Direction = LaminaFlexDirection.Column;
    public Action<LaminaLayout> Children;
}
public enum LaminaFlexDirection
{
    Row,
    Column
}

public enum LaminaFlexAlign
{
    Start,
    Center,
    End,
    SpaceBetween,
    SpaceAround
}

/// <summary>
/// Image
/// </summary>
public record ImageLayout(LaminaImageProps Props) : LaminaLayout(typeof(ImageLayout), Props);
public partial record struct LaminaImageProps() : ILaminaProps
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public Vector2 Size { get; set; } = Vector2.Zero;
    public Vector2 Padding { get; set; } = Vector2.Zero;
    public LaminaFillMode FillMode { get; set; } = LaminaFillMode.FillContainer;
    public string? ImagePath = null;
    public Box ClippingRect = Box.Full;
    public Color Tint = Color.White;
    public Vector4Shorthand BorderRadius = 0;
}

/// <summary>
/// Label
/// </summary>
public record LabelLayout(LaminaLabelProps Props) : LaminaLayout(typeof(LabelLayout), Props);
public partial record struct LaminaLabelProps() : ILaminaProps
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    [Hidden] public Vector2 Size { get; set; } = Vector2.Zero;
    public Vector2 Padding { get; set; } = Vector2.Zero;
    public LaminaFillMode FillMode { get; set; } = LaminaFillMode.None;
    public string Text;
    public string Font = "RobotoMono-Bold";
    public int FontSize = 18;
    public Color Color = Color.Black;
}

/// <summary>  
/// Line
/// </summary>
public record LineLayout(LaminaLineProps Props) : LaminaLayout(typeof(LineLayout), Props);
public partial record struct LaminaLineProps() : ILaminaProps
{
    public Vector2 Position { get; set; } = Vector2.Zero;
    public Vector2 Size { get; set; } = Vector2.Zero;
    public Vector2 Padding { get; set; } = Vector2.Zero;
    public LaminaFillMode FillMode { get; set; } = LaminaFillMode.FillContainer;
    public IReadOnlyList<Vector2> Points;
    public Color Color = Color.Black;
    public int Thickness = 1;
}

[AttributeUsage(AttributeTargets.Property)]
internal class HiddenAttribute : Attribute;
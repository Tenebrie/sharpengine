using System.Drawing;
using Engine.Core.Common;
using Engine.Core.Modules;

namespace Engine.Core.Lamina;

public record FragmentLayout() : LaminaLayout(typeof(LaminaLayout));

public record DivLayout(Vector2 Offset) : LaminaLayout(typeof(DivLayout));
public record HeaderLayout(string Text) : LaminaLayout(typeof(HeaderLayout));
public record LabelLayout(string Text) : LaminaLayout(typeof(LabelLayout));

public record LaminaLayout(Type Type) : ILaminaLayout
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

    public void Div(Vector2 position, Action<LaminaLayout> action) => Add(new DivLayout(position), action);
    public void Header(string text) => Add(new HeaderLayout(text));
    public void Label(string text) => Add(new LabelLayout(text));
    public void Button(string label, Color? backgroundColor = null, Action? onClick = null) => Add(new ButtonLayout(new LaminaButtonProps()
    {
        Label = label,
        BackgroundColor = backgroundColor ?? Color.Gray,
        OnClick = onClick,
    }));
    public void Line(IReadOnlyList<Vector2> points, Color? color = null, int thickness = 1) => Add(new LineLayout(new LaminaLineProps
    {
        Points = points,
        Color = color ?? Color.Black,
        Thickness = thickness,
    }));
}

public record struct LaminaButtonProps
{
    public required string Label;
    public required Color BackgroundColor;
    public required Action? OnClick;
}
public record ButtonLayout(LaminaButtonProps Props) : LaminaLayout(typeof(ButtonLayout));

public record struct LaminaLineProps
{
    public required IReadOnlyList<Vector2> Points;
    public required Color Color;
    public required int Thickness;
}

public record LineLayout(LaminaLineProps Props) : LaminaLayout(typeof(LineLayout));

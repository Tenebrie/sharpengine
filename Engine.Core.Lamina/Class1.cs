using System.Reflection.Emit;
using Engine.Core.Common;
using Engine.Core.Modules;
using JetBrains.Annotations;

namespace Engine.Core.Lamina;

public record FragmentLayout() : LaminaLayout(typeof(LaminaLayout));
public record DivLayout(Vector2 Offset) : LaminaLayout(typeof(DivLayout));
public record HeaderLayout(string Text) : LaminaLayout(typeof(HeaderLayout));
public record LabelLayout(string Text) : LaminaLayout(typeof(LabelLayout));
public record ButtonLayout(string Text, Action? OnClick = null) : LaminaLayout(typeof(ButtonLayout));
public record VStack(List<LaminaLayout> Children) : LaminaLayout(typeof(VStack));
public record HStack(List<LaminaLayout> Children, int Gap = 4) : LaminaLayout(typeof(HStack));

public record LaminaLayout(Type type) : ILaminaLayout
{
    public readonly Type LayoutType = type;
    public readonly List<LaminaLayout> Children = [];
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
    public void Button(string label, Action? onClick = null) => Add(new ButtonLayout(label, onClick));
}

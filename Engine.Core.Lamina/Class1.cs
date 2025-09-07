using Engine.Core.Common;
using JetBrains.Annotations;

namespace Engine.Core.Lamina;

// public abstract record LaminaElement;

public sealed record Header(string Text) : LaminaElement;
public sealed record Button(string Label, Action? OnClick = null) : LaminaElement;
public sealed record CustomElement() : LaminaElement;
public sealed record VStack(List<LaminaElement> Children) : LaminaElement;
public sealed record HStack(List<LaminaElement> Children, int Gap = 4) : LaminaElement;

public record LaminaElement
{
    public readonly List<LaminaElement> Children = [];
    public void Add(LaminaElement w) => Children.Add(w);
    public void Header(string text) => Add(new Header(text));
    public void Button(string label, Action? onClick = null) => Add(new Button(label, onClick));
    public VStack VStack(Action<LaminaElement> view)
    {
        var inner = new LaminaElement();
        view(inner);
        var vstack = new VStack([..inner.Children]);
        Add(vstack);
        return vstack;
    }
    public VStack Floater(Vector3 position, Action<LaminaElement> body)
    {
        var inner = new LaminaElement();
        body(inner);
        var vstack = new VStack([..inner.Children]);
        Add(vstack);
        return vstack;
    }
}

public class Widget
{
    public LaminaElement Root = null!;

    public void Render(Action<LaminaElement> body)
    {
        var scope = new LaminaElement();
        body(scope);
        Root = scope;
    }
    
    public VStack VStack(Action<LaminaElement> body)
    {
        var inner = new LaminaElement();
        body(inner);
        var vstack = new VStack([..inner.Children]);
        return vstack;
    }
}

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
// ReSharper disable once InconsistentNaming
public class OnUpdateUI : Attribute
{
}

public class CustomButton : Widget
{
    
    public record Element : LaminaElement
    {
        
    }
    
    [OnUpdateUI]
    public LaminaElement Render()
    {
        return VStack(v =>
        {
            v.Header("Hello World");
            v.Button("Click Me", () => Console.WriteLine("Button Clicked!"));
            v.Add(new CustomButton.Element());
            v.VStack(Renderers.RenderFloatingButtons);
        });
    }
}

public static class Renderers
{
    private static readonly List<Vector3> Positions = [ new(0,0,0), new(1,1,1), new(2,2,2) ];
    
    public static void RenderFloatingButtons(LaminaElement v)
    {
        foreach (var position in Positions)
        {
            v.Floater(position, v =>
            {
                v.Button("Floating button", () => Console.WriteLine($"Clicked button at {position}"));
            });
        }
    }
}

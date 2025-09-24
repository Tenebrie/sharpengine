using System.Diagnostics.CodeAnalysis;
using Engine.Core.Logging;

namespace Engine.Core.Lamina;

public static class LaminaRendererRepository
{
    private static readonly Dictionary<Type, LaminaWidgetRenderer> RegisteredRenderers = new();

    static LaminaRendererRepository()
    {
        // RegisteredRenderers.Add(typeof(LaminaLayout), new LaminaWidgetRenderer
        // {
        //     LayoutType = typeof(LaminaLayout),
        //     RenderChildren = (LaminaLayout layout) => { },
        //     RenderVisual = (LaminaLayout layout, ILaminaRenderContext context) => { }
        // });
    }

    public static void RegisterRenderer<TLayout, TWidget>() where TLayout : LaminaLayout where TWidget : IWidget
    {
        var data = new LaminaWidgetRenderer
        {
            LayoutType = typeof(TLayout),
            WidgetType = typeof(TWidget)
        };
        RegisteredRenderers.TryAdd(typeof(TLayout), data);
    }

    public static void Unregister<TLayout>() where TLayout : LaminaLayout
    {
        RegisteredRenderers.Remove(typeof(TLayout));
    }
    
    public static bool TryGet(LaminaLayout layout, [MaybeNullWhen(false)] out LaminaWidgetRenderer renderer)
    {
        return RegisteredRenderers.TryGetValue(layout.LayoutType, out renderer);
    }
}
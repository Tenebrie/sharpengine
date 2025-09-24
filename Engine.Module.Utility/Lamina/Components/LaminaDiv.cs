using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using JetBrains.Annotations;

namespace Engine.Module.Utility.Lamina.Components;

[UsedImplicitly]
public partial class LaminaDiv : WidgetComponent
{
    protected override void Render(LaminaLayout layout, ILaminaRenderContext context)
    {
        if (layout is not DivLayout divLayout)
            throw new InvalidOperationException($"Expected layout of type {nameof(DivLayout)}, got {layout.GetType().Name}");
        context.Position += divLayout.Offset;
    }

    protected override void PostRender(LaminaLayout layout, ILaminaRenderContext context)
    {
        if (layout is not DivLayout divLayout)
            throw new InvalidOperationException($"Expected layout of type {nameof(DivLayout)}, got {layout.GetType().Name}");
        context.Position -= divLayout.Offset;
    }
}
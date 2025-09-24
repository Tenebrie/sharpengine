using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using JetBrains.Annotations;

namespace Engine.Module.Utility.Lamina.Components;

[UsedImplicitly]
public partial class LaminaButton : WidgetComponent
{
    [LaminaLayout("Button")]
    public record Layout : ButtonLayout
    {
        public Layout(string text, Action? OnClick = null) : base(text, OnClick)
        {
            Add(new LabelLayout(Text));
        }
    }

    [LaminaRenderer]
    protected override void Render(LaminaLayout layout, ILaminaRenderContext context)
    {
        if (layout is not ButtonLayout buttonLayout)
            throw new ArgumentException($"Expected layout of type {nameof(ButtonLayout)}, got {layout.GetType().Name}");
        context.RenderRequest(new LaminaRenderRequest
        {
            InstanceCount = 1,
            InstanceTransforms = [],
            Material = null!,
            Mesh = null!,
            RenderScript = null!,
            MaterialInstances = []
        });
    }
}
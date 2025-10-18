using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;

namespace Engine.Module.Utility.Lamina.Components;

[LaminaWidget]
public partial class LaminaLabel : LaminaWidgetComponent<LabelLayout>
{
    public override void OnRender(LabelLayout layout, ILaminaRenderContext context)
    {
        context.RenderText(layout.Props.Font,
            layout.Props.FontSize,
            layout.Props.Text,
            layout.Props.Position + WorldTransformNoScale.Position.ToVector2(),
            layout.Props.Color,
            2);
    }
}

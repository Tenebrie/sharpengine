using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;

namespace Engine.Module.Utility.Lamina.Components;

[LaminaWidget]
public partial class LaminaDiv : LaminaWidgetComponent<DivLayout>
{
    public override void OnRender(DivLayout layout, ILaminaRenderContext context)
    {
        context.Position += layout.Props.Position;
    }

    public override void OnPostRender(DivLayout layout, ILaminaRenderContext context)
    {
        context.Position -= layout.Props.Position;
    }
}

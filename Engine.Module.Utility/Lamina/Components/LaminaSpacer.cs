using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;

namespace Engine.Module.Utility.Lamina.Components;

[LaminaWidget]
public partial class LaminaSpacer : LaminaWidgetComponent<SpacerLayout>
{
    public override void OnRender(SpacerLayout layout, ILaminaRenderContext context)
    {
        context.Position += layout.Props.Size;
    }
}
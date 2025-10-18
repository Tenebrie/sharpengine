using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using Engine.Core.Logging;

namespace Engine.Module.Utility.Lamina.Components;

[LaminaWidget]
public partial class LaminaDiv : LaminaWidgetComponent<DivLayout>
{
    public override void OnRender(DivLayout layout, ILaminaRenderContext context)
    {
        var horizontalAlignment = layout.Props.Direction == LaminaDivDirection.Row
            ? layout.Props.JustifyContent
            : layout.Props.AlignContent;
        var verticalAlignment = layout.Props.Direction == LaminaDivDirection.Row
            ? layout.Props.AlignContent
            : layout.Props.JustifyContent;

        Transform.Position = (layout.Props.Position + context.OffsetToParent).ToVector3();
        // Logger.Info(context.Parent);
        // Logger.Info(context.Parent.Transform.Position);
        // context.Position += layout.Props.Position;
    }

    public override void OnRenderChildren(DivLayout layout, ILaminaRenderContext context)
    {
        var horizontalGap = layout.Props.Direction == LaminaDivDirection.Row ? layout.Props.Gap : 0;
        var verticalGap = layout.Props.Direction == LaminaDivDirection.Column ? layout.Props.Gap : 0;

        foreach (var child in Children)
        {
            if (child is WidgetComponent widget)
                widget.PerformRender(context);
            context.ChildrenPosition += (horizontalGap, verticalGap);
        }
    }

    public override void OnPostRender(DivLayout layout, ILaminaRenderContext context)
    {
        // context.Position -= layout.Props.Position;
    }
}

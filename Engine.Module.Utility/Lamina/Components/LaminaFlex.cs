using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;

namespace Engine.Module.Utility.Lamina.Components;

[LaminaWidget]
public partial class LaminaFlex : LaminaWidgetComponent<FlexLayout>
{
    public override void OnPopulateIntrinsics(FlexLayout layout)
    {
        var forcedDir = LaminaFillMode.FillVertical;
        if (layout.Props.Direction == LaminaFlexDirection.Column)
            forcedDir = LaminaFillMode.FillHorizontal;
            
        foreach (var child in layout.Children)
        {
            if (child.SharedProps.FillMode != LaminaFillMode.FillContainer)
                continue;
            
            child.SharedProps.Size = Vector2.Max(child.SharedProps.Size, (64, 64));
            child.SharedProps.FillMode = forcedDir;
        }
    }

    public override void OnRenderChildren(FlexLayout layout, ILaminaRenderContext context)
    {
        var horizontal = layout.Props.Direction == LaminaFlexDirection.Row;
        var horizontalGap = layout.Props.Direction == LaminaFlexDirection.Row ? layout.Props.Gap : 0;
        var verticalGap = layout.Props.Direction == LaminaFlexDirection.Column ? layout.Props.Gap : 0;

        foreach (var child in Children)
        {
            if (child is WidgetComponent widget)
            {
                widget.PerformRender(context);
                context.ChildrenPosition += horizontal ? (widget.Size.X, 0) : (0, widget.Size.Y);
            }
            context.ChildrenPosition += (horizontalGap, verticalGap);
        }
    }

    public override void OnReflowChildren(FlexLayout layout, ILaminaReflowContext context)
    {
        var horizontal = layout.Props.Direction == LaminaFlexDirection.Row;
        var horizontalGap = layout.Props.Direction == LaminaFlexDirection.Row ? layout.Props.Gap : 0;
        var verticalGap = layout.Props.Direction == LaminaFlexDirection.Column ? layout.Props.Gap : 0;

        // Allocate fixed space first
        var sharedSpace = ContentSize;
        foreach (var child in Children)
        {
            if (child is not WidgetComponent widget)
                continue;

            var relevantSize = horizontal ? widget.MinSize.X : widget.MinSize.Y;
            if (relevantSize > 0)
            {
                sharedSpace -= horizontal ? (widget.Size.X, 0) : (0, widget.Size.Y);
            }
        }
        sharedSpace -= horizontalGap * (Children.Count - 1);
        sharedSpace -= verticalGap * (Children.Count - 1);
        sharedSpace.X = Math.Max(0, sharedSpace.X);
        sharedSpace.Y = Math.Max(0, sharedSpace.Y);

        var actualSpaceTakenByChildren = context.SpaceTakenByChildren;
        foreach (var child in Children)
        {
            if (child is WidgetComponent widget)
            {
                context.SpaceTakenByChildren = (0, 0);
                widget.PerformReflow(context);
                var widgetSize = widget.Size;

                context.ChildrenPosition += horizontal ? (widgetSize.X, 0) : (0, widgetSize.Y);
                actualSpaceTakenByChildren += horizontal ? (widgetSize.X, 0) : (0, widgetSize.Y);
            }

            context.ChildrenPosition += (horizontalGap, verticalGap);
            actualSpaceTakenByChildren += horizontalGap;
            
            // Reset context state to actual value
            context.SpaceTakenByChildren = actualSpaceTakenByChildren;
        }
    }

    public override void OnPostReflow(FlexLayout layout, ILaminaReflowContext context)
    {
        // Logger.Info(context.SpaceTakenByChildren + Padding * 2);
        // var size = Size;
        // size.X = Math.Max(100, (context.SpaceTakenByChildren + Padding * 2).X);
        // size.Y = Math.Max(100, (context.SpaceTakenByChildren + Padding * 2).Y);
        // Size = size;
    }
}

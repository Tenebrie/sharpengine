using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using Engine.Core.Logging;

namespace Engine.Module.Utility.Lamina.Components;

[LaminaWidget]
public partial class LaminaLabel : LaminaWidgetComponent<LabelLayout>
{
    private int _renderRequestId = -1;
    
    public override void OnRender(LabelLayout layout, ILaminaRenderContext context)
    {
        var request = new LaminaTextRenderRequest
        {
            Color = layout.Props.Color,
            Font = layout.Props.Font,
            Size = layout.Props.FontSize,
            Position = Position,
            Text = layout.Props.Text,
            ShadowBlur = 2
        };
        Size += context.MeasureText(request);
        MinSize += context.MeasureText(request);
        _renderRequestId = context.RenderText(request);
    }

    public override void OnReflow(LabelLayout layout, ILaminaReflowContext context)
    {
        var request = context.GetTextRequest(_renderRequestId);
        request.Position = WorldTransformNoScale.Position.ToVector2() + context.ChildrenPosition;
        context.SetTextRequest(_renderRequestId, request);
    }
}

using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using Engine.Core.Logging;

namespace Engine.Module.Utility.Lamina.Components;

[LaminaWidget]
public partial class LaminaImage : LaminaWidgetComponent<ImageLayout>
{
    private Material? _material = null;
    private MaterialInstance? _materialInstance = null;
    private int _renderRequestId = -1;

    public override void OnRender(ImageLayout layout, ILaminaRenderContext context)
    {
        if (layout.SharedProps.FillMode == LaminaFillMode.None && layout.SharedProps.Size == Vector2.Zero)
        {
            // Size = Vector2.Max(layout.SharedProps.Size, (64, 64));
        }
        
        if (_material == null || _materialInstance == null)
        {
            _material = MaterialBuilder.CreateFromDisk("Shaders/UserInterface/General").Compile();
            _materialInstance = _material.Instantiate();
        }

        if (_material != null && _materialInstance != null)
        {
            if (layout.Props.ImagePath != null)
                _material.UpdateTexture(Texture.CreateFromDisk(layout.Props.ImagePath));
            _materialInstance.SetTintColor(layout.Props.Tint);
        }
        
        if (_material == null || _materialInstance == null)
            return;

        _renderRequestId = context.RenderRequest(new LaminaRenderRequest
        {
            InstanceCount = 1,
            InstanceTransforms = [],
            Material = _material,
            Mesh = InterfacePlaneMesh.Shared,
            RenderScript = IRenderScript.LaminaWidget,
            MaterialInstances = [_materialInstance.Snapshot()],
            ShaderParams = new LaminaRenderScript.UserData
            {
                BorderRadius = layout.Props.BorderRadius,
            }
        });
    }

    public override void OnReflow(ImageLayout layout, ILaminaReflowContext context)
    {
        if (layout.SharedProps.FillMode == LaminaFillMode.FillHorizontal)
            Size = (context.SpaceAvailable.X, Size.Y);
        else if (layout.SharedProps.FillMode == LaminaFillMode.FillVertical)
            Size = (Size.X, context.SpaceAvailable.Y);
        if (layout.SharedProps.FillMode == LaminaFillMode.None && layout.SharedProps.Size == Vector2.Zero)
        {
            // Size = Vector2.Max(layout.SharedProps.Size, (64, 64));
            // Logger.Info(context.SpaceAvailable);
        }
        // var position = context.OffsetToParent + layout.Props.Position;
        // Position = position.Rounded();
        //
        // if (layout.Props.Size.X == 0 || layout.Props.Size.Y == 0)
        //     Size = context.Parent.ContentSize;
        // else
        //     Size = layout.Props.Size;
        
        var req = context.GetRequest(_renderRequestId);
        var globalPos = WorldTransformOwnScaleOnly.Position;
        // Logger.Log(WorldTransformNoScale.Position);
        req.InstanceTransforms = [WorldTransformOwnScaleOnly.Snapshot()];
        req.ScissorRect = layout.Props.ClippingRect is var rect
            ? new Rect
            {
                Top = (int)(globalPos.Y + rect.Top * Size.Y),
                Left = (int)(globalPos.X + rect.Left * Size.X),
                Right = (int)(globalPos.X + rect.Right * Size.X),
                Bottom = (int)(globalPos.Y + rect.Bottom * Size.Y)
            }
            : null;
        context.SetRequest(_renderRequestId, req);
    }
}

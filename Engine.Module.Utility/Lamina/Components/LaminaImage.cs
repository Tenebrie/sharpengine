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
            ShaderParams = [new LaminaRenderScript.UserData
            {
                BorderRadius = layout.Props.BorderRadius,
            }]
        });
    }

    public override void OnReflow(ImageLayout layout, ILaminaReflowContext context)
    {
        Size = layout.SharedProps.FillMode switch
        {
            LaminaFillMode.FillHorizontal => (context.SpaceAvailable.X, Size.Y),
            LaminaFillMode.FillVertical   => (Size.X, context.SpaceAvailable.Y),
            LaminaFillMode.FillContainer  => context.SpaceAvailable,
            _ => Size
        };

        var req = context.GetRequest(_renderRequestId);
        var globalPos = WorldTransformOwnScaleOnly.Position;
        req.InstanceTransforms = [WorldTransformOwnScaleOnly.Snapshot()];
        var clipSize = Vector2.Min(Size, context.Parent.Size > Vector2.Zero ? context.Parent.Size : Size);
        req.ScissorRect = layout.Props.ClippingRect is var rect
            ? new Rect
            {
                Top = (int)(globalPos.Y + rect.Top * clipSize.Y),
                Left = (int)(globalPos.X + rect.Left * clipSize.X),
                Right = (int)(globalPos.X + rect.Right * clipSize.X),
                Bottom = (int)(globalPos.Y + rect.Bottom * clipSize.Y)
            }
            : null;
        context.SetRequest(_renderRequestId, req);
    }
}

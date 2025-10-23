using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;

namespace Engine.Module.Utility.Lamina.Components;

[LaminaWidget]
public partial class LaminaImage : LaminaWidgetComponent<ImageLayout>
{
    private Material? _material = null;
    private MaterialInstance? _materialInstance = null;

    public override void OnRender(ImageLayout layout, ILaminaRenderContext context)
    {
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
        
        var position = context.OffsetToParent + layout.Props.Position;
        
        Transform = Transform.Identity;
        Transform.Position = position.ToVector3().Rounded();
        Transform.Scale = new Vector3(layout.Props.Size.X, layout.Props.Size.Y, 1.0);
        if (_material == null || _materialInstance == null)
            return;

        var globalPos = WorldTransformNoScale.Position;
        context.RenderRequest(new LaminaRenderRequest
        {
            InstanceCount = 1,
            InstanceTransforms = [WorldTransformNoScale.Snapshot()],
            Material = _material,
            Mesh = InterfacePlaneMesh.Shared,
            RenderScript = IRenderScript.LaminaWidget,
            MaterialInstances = [_materialInstance.Snapshot()],
            ScissorRect = layout.Props.ClippingRect is var rect ? new Rect
            {
                Top = (int)(globalPos.Y + rect.Top * layout.Props.Size.Y),
                Left = (int)(globalPos.X + rect.Left * layout.Props.Size.X),
                Right = (int)(globalPos.X + rect.Right * layout.Props.Size.X),
                Bottom = (int)(globalPos.Y + rect.Bottom * layout.Props.Size.Y)
            } : null,
            ShaderParams = new LaminaRenderScript.UserData
            {
                BorderRadius = layout.Props.BorderRadius,
            }
        });
    }
}

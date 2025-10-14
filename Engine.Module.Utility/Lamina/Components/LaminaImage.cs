using System.Drawing;
using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using Box = Engine.Core.Common.Box;

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
        
        Position = context.Position + layout.Props.Position;
            
        Transform = Transform.Identity;
        Transform.Position = Position.ToVector3();
        Transform.Scale = new Vector3(layout.Props.Size.X, layout.Props.Size.Y, 1.0);
        
        if (_material == null || _materialInstance == null)
            return;
        
        context.RenderRequest(new LaminaRenderRequest
        {
            InstanceCount = 1,
            InstanceTransforms = [Transform.Snapshot()],
            Material = _material,
            Mesh = InterfacePlaneMesh.Shared,
            RenderScript = IRenderScript.Default,
            MaterialInstances = [_materialInstance.Snapshot()],
            ScissorRect = layout.Props.ClippingRect is {} rect ? new Rect
            {
                Top = (int)(rect.Top * layout.Props.Size.Y),
                Left = (int)(rect.Left * layout.Props.Size.X),
                Right = (int)(rect.Right * layout.Props.Size.X),
                Bottom = (int)(rect.Bottom * layout.Props.Size.Y)
            } : null,
        });
    }
}

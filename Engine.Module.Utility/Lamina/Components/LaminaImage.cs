using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using JetBrains.Annotations;

namespace Engine.Module.Utility.Lamina.Components;

[UsedImplicitly]
public partial class LaminaImage : WidgetComponent
{
    private Material? _material = null;
    private MaterialInstance? _materialInstance = null;

    protected override void Render(LaminaLayout layout, ILaminaRenderContext context)
    {
        if (layout is not ImageLayout imageLayout)
            throw new ArgumentException($"Expected layout of type {nameof(ImageLayout)}, got {layout.GetType().Name}");

        if (_material == null || _materialInstance == null)
        {
            _material = MaterialBuilder.CreateFromDisk("Shaders/UserInterface/General").Compile();
            _materialInstance = _material.Instantiate().SetTintColor(imageLayout.Props.Tint);
        }

        if (_material != null && imageLayout.Props.ImagePath != null)
        {
            _material.UpdateTexture(Texture.CreateFromDisk(imageLayout.Props.ImagePath));
        }
        
        Position = context.Position + imageLayout.Props.Position;
            
        Transform = Transform.Identity;
        Transform.Position = Position.ToVector3();
        Transform.Scale = new Vector3(imageLayout.Props.Size.X, imageLayout.Props.Size.Y, 1.0);
        
        if (_material == null)
            return;
        
        context.RenderRequest(new LaminaRenderRequest
        {
            InstanceCount = 1,
            InstanceTransforms = [Transform.Snapshot()],
            Material = _material,
            Mesh = InterfacePlaneMesh.Shared,
            RenderScript = IRenderScript.Default,
            MaterialInstances = [_materialInstance.Snapshot()],
            ScissorRect = imageLayout.Props.ClippingRect is {} rect ? new Rect
            {
                Top = (int)(rect.Top * imageLayout.Props.Size.Y),
                Left = (int)(rect.Left * imageLayout.Props.Size.X),
                Right = (int)(rect.Right * imageLayout.Props.Size.X),
                Bottom = (int)(rect.Bottom * imageLayout.Props.Size.Y)
            } : null,
        });
    }
}
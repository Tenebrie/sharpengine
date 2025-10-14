using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using Engine.Core.Logging;
using Engine.Module.Utility.Services;

namespace Engine.Module.Utility.Lamina.Components;

[LaminaWidget]
public partial class LaminaButton : LaminaWidgetComponent<ButtonLayout>
{
    public override void OnPopulateIntrinsics(ButtonLayout layout)
    {
        layout.Label(text: layout.Props.Label);
    }

    private Material? _material = null;
    private MaterialInstance? _materialInstance = null;

    public override void OnRender(ButtonLayout layout, ILaminaRenderContext context)
    {
        if (_material == null || _materialInstance == null)
        {
            _material = MaterialBuilder.CreateFromDisk("Shaders/UserInterface/General").Compile();
            _materialInstance = _material.Instantiate().SetTintColor(layout.Props.BackgroundColor);
        }
        
        Position = context.Position;
        Size = new Vector2(100, 50);
            
        Transform = Transform.Identity;
        Transform.Position = context.Position.ToVector3();
        
        context.RenderRequest(new LaminaRenderRequest
        {
            InstanceCount = 1,
            InstanceTransforms = [Transform.Snapshot()],
            Material = _material,
            Mesh = InterfacePlaneMesh.Shared,
            RenderScript = IRenderScript.Default,
            MaterialInstances = [_materialInstance.Snapshot()]
        });
    }
    
    [OnInput(InputAction.MouseMove, 1.0, 1.0)]
    protected void OnMouseMove(Vector2 direction)
    {
        Logger.Info(direction);
    }
}


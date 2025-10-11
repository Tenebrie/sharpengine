using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using Engine.Core.Logging;
using Engine.Module.Utility.Services;
using JetBrains.Annotations;

namespace Engine.Module.Utility.Lamina.Components;

[UsedImplicitly]
public partial class LaminaButton : WidgetComponent
{
    protected override void PopulateIntrinsics(LaminaLayout layout)
    {
        if (layout is not ButtonLayout buttonLayout)
            throw new ArgumentException($"Expected layout of type {nameof(ButtonLayout)}, got {layout.GetType().Name}");
        
        layout.Label(text: buttonLayout.Props.Label);
    }

    private Material? _material = null;
    private MaterialInstance? _materialInstance = null;

    protected override void Render(LaminaLayout layout, ILaminaRenderContext context)
    {
        if (layout is not ButtonLayout buttonLayout)
            throw new ArgumentException($"Expected layout of type {nameof(ButtonLayout)}, got {layout.GetType().Name}");

        if (_material == null || _materialInstance == null)
        {
            _material = MaterialBuilder.CreateFromDisk("Shaders/UserInterface/General").Compile();
            _materialInstance = _material.Instantiate().SetTintColor(buttonLayout.Props.BackgroundColor);
        }
        
        Position = context.Position;
        Size = new Vector2(100, 50);
            
        Transform = Transform.Identity;
        // var screenPosition = context.Position / RenderContext.Current.RenderTargetSize - Vector2.One / 2;
        // var scale = Size / RenderContext.Current.RenderTargetSize;
        // Transform.Scale = new Vector3(scale.X, scale.Y, 1.0) * 2;
        // Transform.Position = new Vector3(screenPosition.X, -screenPosition.Y, 0) * 2 + new Vector3(scale.X, -scale.Y, 0.0);
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
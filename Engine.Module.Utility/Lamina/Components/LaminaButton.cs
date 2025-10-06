using System.Drawing;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Renderers;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.Lamina;
using Engine.Core.Logging;
using JetBrains.Annotations;

namespace Engine.Module.Utility.Lamina.Components;

[UsedImplicitly]
public partial class LaminaButton : WidgetComponent
{
    protected override void PopulateIntrinsics(LaminaLayout layout)
    {
        if (layout is not ButtonLayout buttonLayout)
            throw new ArgumentException($"Expected layout of type {nameof(ButtonLayout)}, got {layout.GetType().Name}");
        layout.Add(new LabelLayout(buttonLayout.Text));
    }
    
    private MaterialInstance? _materialInstance = null;

    [LaminaRenderer]
    protected override void Render(LaminaLayout layout, ILaminaRenderContext context)
    {
        if (layout is not ButtonLayout buttonLayout)
            throw new ArgumentException($"Expected layout of type {nameof(ButtonLayout)}, got {layout.GetType().Name}");

        var material = MaterialBuilder.CreateFromDisk("Shaders/UserInterface/General").WithCache().Compile();
        var transform = Transform.Identity;
        if (_materialInstance == null)
        {
            var textureSize = new Vector2(120, 64);
            var scale = textureSize / RenderContext.Current.RenderTargetSize;
            transform.Scale = new Vector3(scale.X, scale.Y, 1.0) * 2;
            _materialInstance = material.Instantiate().SetTintColor(Color.Red);
        }
        context.RenderRequest(new LaminaRenderRequest
        {
            InstanceCount = 1,
            InstanceTransforms = [transform],
            Material = material,
            Mesh = InterfacePlaneMesh.Shared,
            RenderScript = IRenderScript.Default,
            MaterialInstances = [_materialInstance]
        });
    }
}
using System.Drawing;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Input.Attributes;
using Engine.Core.Lamina;
using Engine.Core.Logging;
using Engine.Module.Utility.Services;

namespace Engine.Module.Utility.Lamina.Components;

[LaminaWidget]
public partial class LaminaButton : LaminaWidgetComponent<ButtonLayout>
{
    public override void OnPopulateIntrinsics(ButtonLayout layout)
    {
        layout.Box(children: v =>
        {
            v.Image(tint: Color.FromArgb(120, 0,0,0), borderRadius: 6);
            v.Flex(size: (96, 64), padding: (16, 8), children: v =>
            {
                v.Label(text: layout.Props.Label, color: Color.AliceBlue);
            }); 
            // v.Image(tint: Color.FromArgb(120, 0, 0, 0), clippingRect: Box.FillTop(progress));
            // var textColor = controller.CurrentAbilityIndex == abilityIndex ? Color.Gold : Color.White;
            // v.Label(name, fontSize: 16, color: textColor, position: (4, 4));
            // v.Label(keybind, fontSize: 24, color: textColor, position: (48, 36));
        });
    }

    private Material? _material = null;
    private MaterialInstance? _materialInstance = null;

    public override void OnRender(ButtonLayout layout, ILaminaRenderContext context)
    {
        // if (_material == null || _materialInstance == null)
        // {
            // _material = MaterialBuilder.CreateFromDisk("Shaders/UserInterface/General").Compile();
            // _materialInstance = _material.Instantiate().SetTintColor(layout.Props.BackgroundColor);
        // }
        
        // Position = context.Position;
        // Size = new Vector2(100, 50);
            
        // Transform = Transform.Identity;
        // Transform.Position = context.OffsetToParent.ToVector3();
        
        // context.RenderRequest(new LaminaRenderRequest
        // {
        //     InstanceCount = 1,
        //     InstanceTransforms = [WorldTransformNoScale.Snapshot()],
        //     Material = _material,
        //     Mesh = InterfacePlaneMesh.Shared,
        //     RenderScript = IRenderScript.LaminaWidget,
        //     MaterialInstances = [_materialInstance.Snapshot()],
        //     ShaderParams = new LaminaRenderScript.UserData
        //     {
        //         BorderRadius = layout.Props.BorderRadius,
        //     }
        // });
    }
    
    [OnInput(InputAction.MouseClick)]
    protected void OnMouseClick()
    {
        var inputService = GetService<InputService>();

        var parentPos = GetParent<UserInterfaceComponent>()!.Transform.Position;


        var position = (parentPos + WorldTransformNoScale.Position).ToVector2();
        var clickPos = inputService.GetMousePosition();
        if (clickPos.X >= position.X && clickPos.Y >= position.Y && clickPos.X <= (position + Size).X && clickPos.Y <= (position + Size).Y)
        {
            Logger.Info("Button clicked!");
        }
    }
}


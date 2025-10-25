using System.Drawing;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Lamina;
using Engine.Core.Logging;

namespace User.Game.FirstPerson;

public partial class AbilityHotbar : Actor
{
    [Component] public UserInterfaceComponent CrosshairWidget;
    [Component] public UserInterfaceComponent HotbarWidget;

    [OnReady]
    protected void OnReady()
    {
        CrosshairWidget.SetLayout(v =>
        {
            CrosshairWidget.Size = (32, 32);
            CrosshairWidget.Transform.Position = (Backstage.Window.Size / 2.0).ToVector3() - (16, 16, 0);
            
            v.Image(size: (32, 32), imagePath: "Textures/crosshair.png");
        });
    }

    [OnTimer(Seconds = 0.5, TicksOnce = true)]
    protected void Once()
    {
        CrosshairWidget.SetLayout(v =>
        {
            CrosshairWidget.Size = (32, 32);
            CrosshairWidget.Transform.Position = (Backstage.Window.Size / 2.0).ToVector3() - (16, 16, 0);
            
            v.Image(size: (32, 32), imagePath: "Textures/crosshair.png");
        });
    }

    [OnTimer(Seconds = 1 / 60.0)]
    protected void OnTimer()
    {
        HotbarWidget.SetLayout(v =>
        {
            var controller = FirstPersonAbilityController.All.FirstOrDefault();
            if (controller == null)
                return;
            var screenSize = Backstage.Window.Size;
            HotbarWidget.Transform.Position = (screenSize.X / 2 - HotbarWidget.Size.X / 2, screenSize.Y - HotbarWidget.Size.Y - 16, 0);

            v.Image(tint: Color.FromArgb(120, 0, 0, 0), borderRadius: (4, 20));
            v.Flex(direction: LaminaFlexDirection.Column, padding: (16, 8), gap: 12, children: v =>
            {
                v.Label(text: "Hotbar", fontSize: 24, padding: (0, 0), color: Color.White);
                
                v.Flex(gap: 8, direction: LaminaFlexDirection.Row, children: v =>
                {
                    var abilityCount = controller.AbilityCount;
                    for (var i = 0; i < abilityCount; i++)
                    {
                        var abilityIndex = i;
                        v.Box(v =>
                        {
                            v.Box(size: (64, 64), children: v =>
                            {
                                var ability = controller.GetAbilityAt(abilityIndex);
                                var name = ability == null ? "" : "Name";
                                var keybind = ability == null ? "" : (abilityIndex + 1).ToString();
                        
                                var progress = 1 - controller.GetProgress(abilityIndex);
                                if (ability == null)
                                    progress = 0;
                        
                                if (ability == null)
                                    v.Image(tint: Color.FromArgb(120, 0,0,0), borderRadius: 6);
                                else
                                    v.Image(imagePath: ability.GetIconPath(), borderRadius: 6);
                                v.Image(tint: Color.FromArgb(120, 0, 0, 0), clippingRect: Box.FillTop(progress));
                                var textColor = controller.CurrentAbilityIndex == abilityIndex ? Color.Gold : Color.White;
                                v.Label(name, fontSize: 16, color: textColor, position: (4, 4));
                                v.Label(keybind, fontSize: 24, color: textColor, position: (48, 36));
                            });
                        });
                        
                    }
                });
                
            });
        });
    }
}
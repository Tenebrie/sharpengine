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
            CrosshairWidget.Transform.Position = (Backstage.Window.Size / 2.0).ToVector3();
            
            v.Image(size: (32, 32), imagePath: "Textures/crosshair.png");
        });
    }

    [OnTimer(Seconds = 0.5, TicksOnce = true)]
    protected void Once()
    {
        CrosshairWidget.SetLayout(v =>
        {
            CrosshairWidget.Size = (32, 32);
            CrosshairWidget.Transform.Position = (Backstage.Window.Size / 2.0).ToVector3();
            
            v.Image(size: (32, 32), imagePath: "Textures/crosshair.png");
        });
    }

    [OnTimer(Seconds = 1 / 60.0)]
    protected void OnTimer()
    {
        HotbarWidget.SetLayout(v =>
        {
            var screenSize = Backstage.Window.Size;
            HotbarWidget.Transform.Position = (screenSize.X / 2 - 128 - 32, screenSize.Y - 148, 0);

            var controller = FirstPersonAbilityController.All.First();
            v.Label(text: "Hotbar", fontSize: 24, color: Color.White, position: (16, 8));
            v.Image(tint: Color.FromArgb(120, 0, 0, 0), size: (256 + 48 + 32, 64 + 48 + 8));
            v.Div(position: (16, 40), gap: 64 + 16, direction: LaminaDivDirection.Row, children: v =>
            {
                v.Div(children: v =>
                {
                    var progress = 1 - controller.GetProgress(0);
                    v.Image(size: (64, 64), tint: Color.DarkRed);
                    v.Image(size: (64, 64), tint: Color.FromArgb(120, 0, 0, 0), clippingRect: Box.FillTop(progress));
                    var textColor = controller.CurrentAbilityIndex == 0 ? Color.Gold : Color.White;
                    v.Label("1", fontSize: 24, color: textColor, position: (48, 36));
                    v.Label("Pew-pew", fontSize: 16, color: textColor, position: (4, 4));
                });
                v.Div(children: v =>
                {
                    var progress = 1 - controller.GetProgress(1);
                    v.Image(size: (64, 64), tint: Color.DarkGreen);
                    v.Image(size: (64, 64), tint: Color.FromArgb(120, 0, 0, 0), clippingRect: Box.FillTop(progress));
                    var textColor = controller.CurrentAbilityIndex == 1 ? Color.Gold : Color.White;
                    v.Label("2", fontSize: 24, color: textColor, position: (48, 36));
                    v.Label("Pew-pew", fontSize: 16, color: textColor, position: (4, 4));
                });
                v.Div(children: v =>
                {
                    var progress = 1 - controller.GetProgress(2);
                    v.Image(size: (64, 64), tint: Color.DarkBlue);
                    v.Image(size: (64, 64), tint: Color.FromArgb(120, 0, 0, 0), clippingRect: Box.FillTop(progress));
                    var textColor = controller.CurrentAbilityIndex == 2 ? Color.Gold : Color.White;
                    v.Label("3", fontSize: 24, color: textColor, position: (48, 36));
                    v.Label("Pew-pew", fontSize: 16, color: textColor, position: (4, 4));
                });
                v.Div(children: v =>
                {
                    var progress = 1 - controller.GetProgress(3);
                    v.Image(size: (64, 64), tint: Color.DodgerBlue);
                    v.Image(size: (64, 64), tint: Color.FromArgb(120, 0, 0, 0), clippingRect: Box.FillTop(progress));
                    var textColor = controller.CurrentAbilityIndex == 3 ? Color.DarkGoldenrod : Color.White;
                    v.Label("4", fontSize: 24, color: textColor, position: (48, 36));
                    v.Label("Clean\npew-pews", fontSize: 14, color: textColor, position: (4, 4));
                });
            });
        });
    }
}
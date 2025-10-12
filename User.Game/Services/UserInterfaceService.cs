using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Services;

public partial class UserInterfaceService : Service
{
    [Component] 
    protected UserInterfaceComponent UserInterface;

    double progress = 0;
    
    [OnTimer(Seconds = 0.01)]
    protected void OnUpdate()
    {
        progress += 0.01;
        if (progress > 1)
        {
            progress = 0;
        }
        UserInterface.Transform.Position = (250, 250, 0);
        UserInterface.SetLayout(v =>
        {
            v.Div(position: (0, 0), v =>
            {
                v.Image(imagePath: "Textures/godot.png", clippingRect: Box.FillFancy(progress));
            });
        });
    }
}
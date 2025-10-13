using System.Drawing;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using User.Game.Player;

namespace User.Game.Services;

public partial class UserInterfaceService : Service
{
    [Component] 
    protected UserInterfaceComponent UserInterface;

    private int _lastSeenLevel = 1;
    private double _flashTimer = 0;
    private double _smoothProgress = 0.0;
    
    [OnTimer(Seconds = 0.01)]
    protected void OnUpdate(double deltaTime)
    {
        var player = PlayerCharacter.All.FirstOrDefault();
        if (player == null)
            return;

        var progress = player.Experience.CurrentPercentage;
        if (_flashTimer > 0.0)
        {
            _flashTimer -= deltaTime * 5.0;
            if (_flashTimer < 0.0)
                _flashTimer = 0.0;
        }
        if (player.Experience.Level > _lastSeenLevel)
        {
            _flashTimer = 1.0;
            _smoothProgress = 0.0;
            _lastSeenLevel = player.Experience.Level;
        }

        _smoothProgress += (progress - _smoothProgress) * Math.Min(1.0, deltaTime * 75.0);

        var windowSize = Backstage.Window.GetScaledFramebufferSize();
        UserInterface.Transform.Position = (windowSize.X / 2.0 - 256, windowSize.Y - 80, 0);
        UserInterface.SetLayout(v =>
        {
            v.Div(position: (0, 0), v =>
            {
                var label =
                    $"Level {player.Experience.Level}: {player.Experience.Experience} / {player.Experience.ExpForNextLevel}";
                v.Image(size: (512, 48), tint: Color.Gray);
                v.Image(size: (512, 48), clippingRect: Box.FillRight(_smoothProgress), tint: Color.DarkRed);
                v.Image(size: (512, 48), tint: Color.FromArgb((int)(_flashTimer * 255), Color.DarkGoldenrod));
                v.Label(label, color: Color.White, fontSize: 42, position: (20, 2));
            });
        });
    }
}

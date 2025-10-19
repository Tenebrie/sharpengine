using System.Drawing;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Extensions;
using JetBrains.Annotations;
using User.Game.Player;

namespace User.Game.Actors.UserInterface;

[UsedImplicitly]
public partial class ExperienceBarWidget : Actor
{
    [Component] private UserInterfaceComponent _userInterface;
    
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
        
        _userInterface.SetLayout(v =>
        {
            var windowSize = Backstage.Window.FramebufferSize;
            _userInterface.Transform.Position = (windowSize.X / 2.0 - 256, windowSize.Y - 80, 0);
            
            var exp = Math.Floor(player.Experience.Experience);
            v.Div(position: (0, 0), children: v =>
            {
                var leftLabel = player.Experience.PrestigeLevel > 0 ?
                    $"Prestige {player.Experience.PrestigeLevel}" :
                    $"Level {player.Experience.Level}";
                var label =
                    $"{leftLabel}: {exp} / {player.Experience.ExpForNextLevel}";

                var fontSize = player.Experience.PrestigeLevel > 0 ? 30 : 42;
                var prestigeOffset = player.Experience.PrestigeLevel > 0 ? new Vector2(0, 6) : new Vector2(0, 0);
                v.Image(size: (512, 48), tint: Color.Gray);
                v.Image(size: (512, 48), clippingRect: Box.FillRight(_smoothProgress), tint: Color.DarkRed);
                v.Image(size: (512, 48), tint: Color.FromArgb((int)(_flashTimer * 255), Color.DarkGoldenrod));
                v.Label(label, color: Color.White, fontSize: fontSize, position: (20, 2) + prestigeOffset);
            });
        });
    }
}
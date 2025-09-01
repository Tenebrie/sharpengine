using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Common;
using Engine.Core.Geometry.Intersections;
using Engine.Core.Geometry.Shapes;
using Engine.Core.Logging;
using JetBrains.Annotations;
using User.Game.Actors.BasicEnemies;

namespace User.Game.Player.Abilities.Definitions;

[UsedImplicitly]
public partial class LightningStrikeAbility : ActorComponent, IAbility
{
    private const double CooldownTime = 0.2;
    private double _cooldownRemaining = 0.0;
    
    public void OnCast()
    {
        if (_cooldownRemaining > 0)
            return;
        var mousePos = GetService<InputService>().GetMousePosition();
        
        // Get the intersection point with the ground plane (Y=0)
        if (!Raycast.IntersectPlane(Backstage.ActiveCamera, PlaneShape.FromNormal(Vector3.Up), mousePos, out var targetPoint))
            return;
            
        var effect = CreateActor<LightningStrikeEffect>();
        effect.Transform.RotateAroundLocal(Vector3.Right, -15.0f);
        effect.Transform.Position = targetPoint + new Vector3(0, 0.1, 0);
        effect.Transform.Rescale(75);
        
        var targets = BasicEnemy.All
            .Where(enemy => enemy.WorldTransform.Position.DistanceTo(targetPoint) < 25)
            .ToArray();
        foreach (var hitEnemy in targets)
        {
            const int baseDamage = 220;
            var distanceMultiplier = 1.0 - hitEnemy.WorldTransform.Position.DistanceTo(targetPoint) / 25;
            hitEnemy.DealDamage(baseDamage * distanceMultiplier);
        }
        
        _cooldownRemaining = CooldownTime;
    }

    public void OnCooldownReduce(double deltaTime)
    {
        if (_cooldownRemaining <= 0.0)
            return;
        _cooldownRemaining -= deltaTime;
    }
}
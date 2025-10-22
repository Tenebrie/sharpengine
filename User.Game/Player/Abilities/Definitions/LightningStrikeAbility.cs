using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Common;
using Engine.Core.Geometry.Intersections;
using Engine.Core.Geometry.Shapes;
using Engine.Core.Input;
using JetBrains.Annotations;
using User.Game.Actors.BasicEnemies;
using User.Game.Player.PlayerAttributes;

namespace User.Game.Player.Abilities.Definitions;

[UsedImplicitly]
public partial class LightningStrikeAbility : ActorComponent, IAbility
{
    private const double CooldownTime = 0.9;
    private double _cooldownRemaining = 0.0;

    public bool Ready => _cooldownRemaining <= 0.0;
    public void OnCast()
    {
        if (_cooldownRemaining > 0)
            return;

        if (GetService<InputService>().UserInputMode == UserInputMode.KeyboardAndMouse)
        {
            var mousePos = GetService<InputService>().GetMousePosition();

            // Get the intersection point with the ground plane (Y=0)
            if (Raycast.IntersectPlane(Backstage.ActiveCamera, PlaneShape.FromNormal(Vector3.Up), mousePos, out var targetPoint))
                OnAutoCast(targetPoint);
        }
        else
        {
            var parent = GetParent<PlayerCharacter>();
            var closestEnemy = BasicEnemy.Alive
                .Where(enemy => enemy.WorldTransform.Position.DistanceTo(parent!.WorldTransform.Position) < 100)
                .OrderBy(enemy => enemy.WorldTransform.Position.DistanceTo(parent!.WorldTransform.Position))
                .FirstOrDefault();
            if (closestEnemy == null)
                return;
            OnAutoCast(closestEnemy.WorldTransform.Position);
        }
    }

    public void OnAutoCast(Vector3 targetPoint)
    {
        var effect = CreateActor<LightningStrikeEffect>();
        effect.Transform.RotateAroundLocal(Vector3.Right, -15.0f);
        effect.Transform.Position = targetPoint + new Vector3(0, 0.1, 0);

        var player = PlayerCharacter.All.First();
        var maxDist = 25 * (1.0 + player.Attributes.Get(AttributeType.LightningArea) * 0.1);
        effect.Transform.Rescale(maxDist * 3);
        var targets = BasicEnemy.All
            .Where(enemy => enemy.WorldTransform.Position.DistanceTo(targetPoint) < maxDist)
            .ToArray();
        foreach (var hitEnemy in targets)
        {
            const int baseDamage = 220;
            var distanceMultiplier = 1.0 - hitEnemy.WorldTransform.Position.DistanceTo(targetPoint) / maxDist;

            var damageBoost = 1 + player.Attributes.Power * 0.01;
            hitEnemy.DealDamage(baseDamage * distanceMultiplier * damageBoost);
        }
        
        _cooldownRemaining = CooldownTime;
    }
    
    public void OnCooldownReduce(double deltaTime)
    {
        if (_cooldownRemaining <= 0.0)
            return;
        _cooldownRemaining -= deltaTime;
    }
    
    public double GetCooldownProgress()
    {
        return Math.Clamp(1.0 - (_cooldownRemaining / CooldownTime), 0.0, 1.0);
    }
}
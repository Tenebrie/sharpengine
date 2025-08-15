using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Makers;
using JetBrains.Annotations;
using User.Game.Actors;

namespace User.Game.Player.Abilities.Definitions;

[UsedImplicitly]
public partial class PiercingBladeAbility : ActorComponent, IAbility
{
    private const double CooldownTime = 0.08;
    private double _cooldownRemaining = 0.0;
    
    public void OnCast()
    {
        if (_cooldownRemaining > 0)
            return;
        
        var projectile = CreateActor<BasicProjectile>();
        projectile.Transform.Position = WorldTransform.Position;
        
        var forwardVector = Vector3.Forward;
        var mousePos = GetService<InputService>().GetMousePosition();
        var window = Backstage.GetWindow().Size;
        var value = new Vector3(mousePos.X - window.X / 2.0, 0, mousePos.Y - window.Y / 2.0).Normalized();
        var dotProduct = value.DotProduct(forwardVector);
        var crossProduct = value.CrossProduct(forwardVector);
        var difference = Math.Atan2(crossProduct.Y, dotProduct);
        projectile.Transform.Rotation = QuatMakers.FromRotationRadians(0, difference, 0);

        projectile.PhysicsComponent.LinearVelocity = projectile.Transform.Basis.TransformVector(Vector3.Forward) * 200.0;
        _cooldownRemaining = CooldownTime;
    }

    public void OnCooldownReduce(double deltaTime)
    {
        if (_cooldownRemaining <= 0.0)
            return;
        _cooldownRemaining -= deltaTime;
    }
}

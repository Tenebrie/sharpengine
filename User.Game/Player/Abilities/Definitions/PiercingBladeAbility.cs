using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Geometry.Intersections;
using Engine.Core.Geometry.Shapes;
using Engine.Core.Makers;
using JetBrains.Annotations;
using User.Game.Actors;
using User.Game.Player.PlayerAttributes;

namespace User.Game.Player.Abilities.Definitions;

[UsedImplicitly]
public partial class PiercingBladeAbility : ActorComponent, IAbility
{
    private const double CooldownTime = 0.4;
    private double _cooldownRemaining = 0.0;
    
    public bool Ready => _cooldownRemaining <= 0.0;
    public void OnCast()
    {
        if (_cooldownRemaining > 0)
            return;
        
        var mousePos = GetService<InputService>().GetMousePosition();
        // Get the intersection point with the ground plane (Y=0)
        if (!Raycast.IntersectPlane(Backstage.ActiveCamera, PlaneShape.FromNormal(Vector3.Up), mousePos, out var targetPoint))
            return;

        OnAutoCast(targetPoint);
    }
    
    public void OnAutoCast(Vector3 targetPoint)
    {
        var forwardVector = Vector3.Forward;
        var value = targetPoint - WorldTransform.Position;
        var dotProduct = value.DotProduct(forwardVector);
        var crossProduct = value.CrossProduct(forwardVector);
        var difference = Math.Atan2(crossProduct.Y, dotProduct);
        
        var randomRoll = Random.Shared.NextDouble();
        var bladesTossed = Math.Floor(PlayerCharacter.All.First().Attributes.Get(AttributeType.BladeExtraChance) * 0.1 + randomRoll)+ 1;
        for (var i = 0; i < Math.Floor(bladesTossed); i++)
        {
            var index = i - (bladesTossed - 1) / 2.0;
            var projectile = CreateActor<BasicProjectile>();
            projectile.Transform.Position = WorldTransform.Position;
            projectile.Transform.Rotation = QuatMakers.FromRotationRadians(0, difference + Math.PI / 16 * index, 0);
        
            projectile.Damage *= 1 + PlayerCharacter.All.First().Attributes.Get(AttributeType.Power) * 0.01;
            projectile.MaxPierce += (int)PlayerCharacter.All.First().Attributes.Get(AttributeType.BladePierceCount);

            projectile.PhysicsComponent.LinearVelocity = projectile.Transform.Basis.TransformVector(Vector3.Forward) * 200.0;
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

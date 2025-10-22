using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using JetBrains.Annotations;
using User.Game.Actors;
using User.Game.Player.Abilities;
using User.Game.Player.PlayerAttributes;

namespace User.Game.FirstPerson;

[UsedImplicitly]
public partial class FirstPersonClearAbility : ActorComponent, IAbility
{
    private const double CooldownTime = 0.3;
    private double _cooldownRemaining = 0.0;
    
    public bool Ready => _cooldownRemaining <= 0.0;
    public void OnCast()
    {
        if (_cooldownRemaining > 0)
            return;
        
        OnAutoCast(Vector3.Zero);
    }
    
    public void OnAutoCast(Vector3 position)
    {
        var direction = FirstPersonPlayer.All.First().WorldTransform.Basis;
        
        var projectile = CreateActor<CleaningProjectile>();
        projectile.Transform.Position = WorldTransform.Position + direction.TransformVector((0.25, -0.25, 0));
        projectile.Transform.Basis = direction;
        
        projectile.PhysicsComponent.LinearVelocity = projectile.Transform.Basis.TransformVector(Vector3.Forward) * 200.0;
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

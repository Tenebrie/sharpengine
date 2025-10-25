using System.Drawing;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Geometry.Intersections;
using Engine.Core.Geometry.Shapes;
using Engine.Core.Input;
using JetBrains.Annotations;
using User.Game.Actors;
using User.Game.Actors.BasicEnemies;
using User.Game.Player;
using User.Game.Player.Abilities;
using User.Game.Player.PlayerAttributes;

namespace User.Game.FirstPerson;

[UsedImplicitly]
public partial class FirstPersonAttackAbility : BaseAbility
{
    private const double CooldownTime = 0.02;
    private double _cooldownRemaining = 0.0;
    
    public override bool Ready => _cooldownRemaining <= 0.0;
    public override void OnCast()
    {
        if (_cooldownRemaining > 0)
            return;
        
        OnAutoCast(Vector3.Right);
    }

    public Color ProjectileTint = Color.White;
    
    public override void OnAutoCast(Vector3 targetPoint)
    {
        var direction = FirstPersonPlayer.All.First().WorldTransform.Basis;
        
        var randomRoll = Random.Shared.NextDouble();
        var bladesTossed = Math.Floor(FirstPersonPlayer.All.First().Attributes.Get(AttributeType.BladeExtraChance) * 0.1 + randomRoll)+ 1;
        for (var i = 0; i < Math.Floor(bladesTossed); i++)
        {
            var projectile = CreateActor<BasicProjectile>();
            projectile.MeshComponent.MaterialInstance.SetTintColor(ProjectileTint);
            projectile.Transform.Position = WorldTransform.Position + direction.TransformVector((0.25, -0.25, 0));
            projectile.Transform.Basis = direction;
        
            projectile.Damage *= 1 + FirstPersonPlayer.All.First().Attributes.Get(AttributeType.Power) * 0.01;
            projectile.MaxPierce += (int)FirstPersonPlayer.All.First().Attributes.Get(AttributeType.BladePierceCount);

            projectile.PhysicsComponent.LinearVelocity = projectile.Transform.Basis.TransformVector(Vector3.Forward) * 200.0;
        }
        _cooldownRemaining = CooldownTime;
    }

    public override void OnCooldownReduce(double deltaTime)
    {
        if (_cooldownRemaining <= 0.0)
            return;
        _cooldownRemaining -= deltaTime;
    }

    public override double GetCooldownProgress()
    {
        return Math.Clamp(1.0 - (_cooldownRemaining / CooldownTime), 0.0, 1.0);
    }
}

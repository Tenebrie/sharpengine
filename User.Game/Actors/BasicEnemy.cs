using System.Drawing;
using Engine.Core.Assets.Materials;
using Engine.Core.Common;
using Engine.Core.Communication.Groups;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;
using User.Game.Services;

namespace User.Game.Actors;

public partial class BasicEnemy : ActorInstance
{
    [DefaultGroup] public static readonly Group<BasicEnemy> All = new(); 
    [Component] public PhysicsComponent Physics;
    [Component] public ColliderSphereComponent ColliderSphere;

    public double Health { get; set; } = 200.0;
    public double MaxHealth { get; set; } = 200.0;
    public bool IsDying { get; private set; } = false;
    public double DyingTime { get; set; } = 0.0;
    public double DamageFlashTime { get; set; } = 0.0;
    public double WhiteFlashTime { get; set; } = 0.0;
    public double WhiteFlashMultiplier { get; set; } = 1.0;
    public Vector3 DeathDropRandom { get; set; } = (Vector3.Random - 0.5) * 2;

    public void DealDamage(double damage)
    { 
        if (IsDying)
            return;
        Health -= damage;
        DamageFlashTime += damage / MaxHealth / 2;
        WhiteFlashTime = 0.05;
        WhiteFlashMultiplier = 1.0 + 30.0 * (damage / MaxHealth);
        if (Health > 0)
            return;

        IsDying = true;
        Physics.AngularVelocity += DeathDropRandom * 0.5 * 360.0;
        GetService<ExperienceDropService>().SpawnExperienceDrop(WorldTransform.Position, 10.0);
    }
    
    [OnReady]
    protected void OnReady()
    {
        ColliderSphere.Radius = 2;
        MaterialInstance.LoadTexture(Texture.CreateFromDisk("Textures/metal-albedo.png"));
    }

    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        if (WhiteFlashTime > 0.0)
        {
            WhiteFlashTime = Math.Clamp(WhiteFlashTime - deltaTime, 0.0, 1.0);
            MaterialInstance.SetTintColor(
                Color.White, WhiteFlashMultiplier
            );
        }
        else if (DamageFlashTime > 0.0)
        {
            if (!IsDying)
                DamageFlashTime = Math.Clamp(DamageFlashTime - deltaTime, 0.0, 1.0);
            MaterialInstance.SetTintColor(
                Color.FromArgb(
                    (int)(255),
                    (int)(255 * (1.0 - DamageFlashTime)),
                    (int)(255 * (1.0 - DamageFlashTime)))
            );
            MaterialInstance.SetOpacity(2.0 - DyingTime * 2.0);
        }

        if (!IsDying)
            return;
         
        DyingTime += deltaTime * 0.25;
        // Transform.Rotate(DeathDropRandom * 360.0 * deltaTime);
        if (DyingTime >= 1.0)
        {
            QueueFree();
            return;
        }

        MaterialInstance.SetTintColor(
            Color.FromArgb(
                (int)(255),
                (int)(0),
                (int)(0))
        );
        MaterialInstance.SetOpacity(2.0 - DyingTime * 2.0);
    }
}
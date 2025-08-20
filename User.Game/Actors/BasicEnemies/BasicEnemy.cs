using System.Drawing;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.Communication.Groups;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using User.Game.Services;

namespace User.Game.Actors.BasicEnemies;

public partial class BasicEnemy : ActorInstance
{
    [DefaultGroup] public static readonly Group<BasicEnemy> All = new(); 
    [Component] public PhysicsComponent Physics;
    [Component] public ColliderSphereComponent ColliderSphere;
    // [Component] public StaticMeshComponent Mesh;

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
        WhiteFlashTime = 0.08;
        WhiteFlashMultiplier = 1.0 + 50.0 * (damage / MaxHealth);
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
        
        // SpaceshipFlames.Mesh = PlaneMesh.Shared;
        // SpaceshipFlames.Material = Material.CreateFromDisk("Assets/Shaders/cube");

        for (var i = 0; i < 4; i++)
        {
            var flames = CreateComponent<SpaceshipFlamesComponent>();
            flames.Transform.Rescale(1, 10, 10);
            flames.Transform.Rotate(0, 180, 0);

            switch (i)
            {
                case 0:
                    flames.Transform.TranslateGlobal(0.93, 0.3, 3.9);
                    flames.BumpAnimation();
                    break;
                case 1:
                    flames.Transform.TranslateGlobal(-1.08, 0.3, 3.9);
                    flames.BumpAnimation();
                    break;
                case 2:
                    flames.Transform.TranslateGlobal(0.93, -0.3, 3.9);
                    break;
                default:
                    flames.Transform.TranslateGlobal(-1.08, -0.3, 3.9);
                    break;
            }
        }
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
        
        if (WhiteFlashTime > 0.0)
            return;

        MaterialInstance.SetTintColor(
            Color.FromArgb(
                (int)(255),
                (int)(0),
                (int)(0))
        );
        MaterialInstance.SetOpacity(2.0 - DyingTime * 2.0);
    }
}
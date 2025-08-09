using System.Drawing;
using Engine.Core.Assets.Materials;
using Engine.Core.Common;
using Engine.Core.Communication.Groups;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Actors;

public partial class BasicEnemy : ActorInstance
{
    [DefaultGroup] public static readonly Group<BasicEnemy> All = new(); 
    [Component] public PhysicsComponent Physics;
    [Component] public ColliderSphereComponent ColliderSphere;

    public double Health { get; set; } = 200.0;
    public double MaxHealth { get; set; } = 200.0;
    public bool IsDying { get; set; } = false;
    public double DyingTime { get; set; } = 0.0;
    public double DamageFlashTime { get; set; } = 0.0;
    public double WhiteFlashTime { get; set; } = 0.0;
    public double WhiteFlashMultiplier { get; set; } = 1.0;

    public void DealDamage(double damage)
    {
        if (IsDying)
            return;
        Health -= damage;
        DamageFlashTime += damage / MaxHealth / 2;
        WhiteFlashTime = 0.05;
        WhiteFlashMultiplier = 2.0 + damage / MaxHealth;
        if (Health <= 0)
            IsDying = true;
    }

    [OnUpdate]
    protected void OnUpdate(double delta)
    {
        if (WhiteFlashTime > 0.0)
        {
            WhiteFlashTime = Math.Clamp(WhiteFlashTime - delta, 0.0, 1.0);
            MaterialInstance.SetTintColor(
                Color.White, WhiteFlashMultiplier
            );
        }
        else if (DamageFlashTime > 0.0)
        {
            if (!IsDying)
                DamageFlashTime = Math.Clamp(DamageFlashTime - delta, 0.0, 1.0);
            MaterialInstance.SetTintColor(
                Color.FromArgb(
                    (int)(255),
                    (int)(255 * (1.0 - DamageFlashTime)),
                    (int)(255 * (1.0 - DamageFlashTime)))
            );
            MaterialInstance.SetOpacity(2.0 - DyingTime * 2.0);
        }
        
        if (IsDying)
        {
            DyingTime += delta * 1.0;
            Transform.RotateAroundGlobal(Vector3.Up, 25 * delta);
            if (DyingTime >= 1.0)
            {
                QueueFree();
                return;
            }

            MaterialInstance.SetOpacity(2.0 - DyingTime * 2.0);
        }
    }

    [OnReady]
    protected void OnReady()
    {
        ColliderSphere.Radius = 0.5;
        MaterialInstance.LoadTexture(Texture.CreateFromDisk("Textures/godot.png"));
    }
}
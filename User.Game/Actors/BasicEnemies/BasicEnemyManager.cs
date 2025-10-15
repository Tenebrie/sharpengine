using System.Drawing;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Lamina;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Extensions;
using Engine.Core.Logging;
using Engine.Core.Profiling.Attributes;
using User.Game.Player;

namespace User.Game.Actors.BasicEnemies;

public partial class BasicEnemyManager : Actor
{
    [Component] public InstancedActorComponent<BasicEnemy> InstanceManager;
    public List<BasicEnemy> Instances { get; } = [];

    [Component] protected UserInterfaceComponent EvolutionFactorWidget;
    
    [OnReady]
    protected void OnReady()
    {
        // if (!AssetManager.Meshes.TryGet("Assets/Virtual/BasicEnemy", out var mesh))
        // {
        //     mesh = TessellatedPlaneMesh.CreateWithoutCache();
        //     AssetManager.Meshes.Put("Assets/Virtual/BasicEnemy", mesh);
        // }
        InstanceManager.InstanceStaticMesh = StaticMesh.CreateFromDisk("Meshes/invader01-crab.obj");
        InstanceManager.InstanceMaterial = MaterialBuilder
            .CreateFromDisk("Shaders/cube")
            .SetTexture(Texture.CreateFromDisk("Textures/metal-albedo.png"))
            .AsSharedMaterial()
            .Compile();
        // InstanceManager.Material =
        //     MaterialBuilder.Begin(typeof(BasicEnemyManager)).SetSamplingTexture(false).Compile();
            // .CreateFromDisk("Meshes/BillboardSprite/BillboardSprite");
            // .Instantiate()
            // .LoadTexture(Texture.CreateFromDisk("Textures/godot.png"));
            
        var windowSize = Backstage.Window.GetScaledFramebufferSize();
        EvolutionFactorWidget.Transform.Position = (windowSize.X / 2.0 - 256, windowSize.Y - 180, 0);
        EvolutionFactorWidget.BackgroundColor = Color.FromArgb(128, 0, 0, 0);
        EvolutionFactorWidget.Size = (512, 90);
        EvolutionFactorWidget.SetLayout(v =>
        {
            v.Div(position: (10, 0), children: v =>
            {
                v.Label($"Evolution Factor: {evolutionFactor:F2}", fontSize: 28, color: Color.White);
            });
            v.Div(position: (10, 32), children: v =>
            {
                v.Label($"Enemy Count: {InstanceManager.InstanceCount} / {500}", fontSize: 28, color: Color.White);
            });
            v.Div(position: (10, 58), children: v =>
            {
                v.Label($"^ If it's full, you lose!", fontSize: 24, color: Color.DarkGray);
            });
        });
    }
    
    private double evolutionFactor = 0.25;

    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        evolutionFactor += deltaTime * 0.02 * (1.0 + evolutionFactor * 0.01);
    }
    
    private int _enemiesQueued = 0;
    [OnTimer(Seconds = 0.17f)]
    protected void SpawnEnemy()
    {
        const int maxEnemies = 500;
        EvolutionFactorWidget.SetLayout(v =>
        {
            v.Div(position: (10, 0), children: v =>
            {
                v.Label($"Evolution Factor: {evolutionFactor:F2}", fontSize: 28, color: Color.White);
            });
            v.Div(position: (10, 32), children: v =>
            {
                var color = Color.Green;
                if (maxEnemies - InstanceManager.InstanceCount < 300)
                    color = Color.Yellow;
                if (maxEnemies - InstanceManager.InstanceCount < 200)
                    color = Color.Orange;
                if (maxEnemies - InstanceManager.InstanceCount < 100)
                    color = Color.Red;
                v.Label($"Enemy Count: {InstanceManager.InstanceCount} / {maxEnemies}", fontSize: 28, color: color);
            });
            v.Div(position: (10, 58), children: v =>
            {
                v.Label($"^ If it's full, you lose!", fontSize: 21, color: Color.DarkGray);
            });
        });
        
        if (InstanceManager.InstanceCount >= maxEnemies)
            return;

        var player = ParentScene.Actors.OfType<PlayerCharacter>().FirstOrDefault();
        if (player is null) 
            return;
        
        var enemiesWantToSpawn = Math.Min(10, 1 + (int)(evolutionFactor * 0.3));
        _enemiesQueued += Math.Min(enemiesWantToSpawn, maxEnemies - InstanceManager.InstanceCount - _enemiesQueued);
        var enemiesSpawned = Math.Min(_enemiesQueued, 3);
        
        for (var i = 0; i < enemiesSpawned; i++)
        {
            var instance = InstanceManager.CreateInstance();
            var transform = instance.Transform;
            transform.Rotate(0, Random.Shared.NextDouble() * 360, 0);
            transform.TranslateLocal(200, 0, 0);
            transform.TranslateGlobal(player.WorldTransform.Position);
            
            instance.Health *= evolutionFactor;
        
            // TODO: Understand why rotation is affected by scale
            // transform.Scale = new Vector3(2,2,2);
            // transform.Rotation = Quaternion.Identity;
            // transform.RotateAroundLocal(Vector3.Pitch, -90);
            transform.Rotation = Quaternion.Identity;
            transform.TranslateGlobal(0, Random.Shared.NextDouble() * 1.0, 0);
            // transform.RotateAroundLocal(Vector3.Right, -2);
            transform.Rescale(1.5, 1.5, 1.5);

            if (evolutionFactor > 1 && Random.Shared.NextDouble() < 0.02 + evolutionFactor * 0.002)
            {
                transform.Rescale(1.5,1.5,1.5);
                instance.MakeElite();
                
                if (evolutionFactor > 3 && Random.Shared.NextDouble() < 0.02 + evolutionFactor * 0.01)
                {
                    transform.Rescale(1.5,1.5,1.5);
                    instance.MakeUltraElite();
                }
            }
        }
        _enemiesQueued -= enemiesSpawned;
    }
    
    [OnTimer(Seconds = 0.10f)]
    protected void OnUpdateEnemies(double deltaTime)
    {
        var player = ParentScene.Actors.OfType<PlayerCharacter>().FirstOrDefault();
        if (player is null)
            return;

        foreach (var enemy in InstanceManager.Instances)
        {
            if (enemy.IsDying)
                continue;
            enemy.Transform.LookAt(player.WorldTransform.Position, Vector3.Up);
        }
    }
    
    [OnTimer(Seconds = 0.15f)]
    protected void OnSlowUpdate(double deltaTime)
    {
        var player = ParentScene.Actors.OfType<PlayerCharacter>().FirstOrDefault();
        if (player is null)
            return;

        const double movementSpeed = 15.0;
        foreach (var enemy in InstanceManager.Instances.ToArray())
        {
            if (enemy.IsDying)
            {
                enemy.Physics.LinearVelocity += Vector3.Down * deltaTime * 15.0;
                enemy.Physics.AngularVelocity -= enemy.DeathDropRandom * deltaTime * 25.0;
                continue;
            }
            enemy.Physics.LinearVelocity = player.WorldTransform.Position - enemy.WorldTransform.Position;
            enemy.Physics.LinearVelocity = enemy.Physics.LinearVelocity.Normalized();
            enemy.Physics.LinearVelocity *= movementSpeed;
            
            var distanceToPlayer = enemy.Transform.Position.DistanceTo(player.WorldTransform.Position);
            if (distanceToPlayer < 250)
                continue;

            enemy.Transform.Position = player.WorldTransform.Position + (player.WorldTransform.Position - enemy.Transform.Position).Normalized() * (Random.Shared.NextDouble() * 50.0 + 100);
            // enemy.QueueFree();
            // _enemiesQueued += 1;
        }
    }
}

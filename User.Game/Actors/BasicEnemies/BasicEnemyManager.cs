using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using User.Game.Player;

namespace User.Game.Actors.BasicEnemies;

public partial class BasicEnemyManager : Actor
{
    // [Component]
    // public InstancedActorComponent<BasicEnemy> InstanceManager;
    public List<BasicEnemy> Instances { get; } = [];
    
    [OnReady]
    protected void OnReady()
    {
        // if (!AssetManager.Meshes.TryGet("Assets/Virtual/BasicEnemy", out var mesh))
        // {
        //     mesh = TessellatedPlaneMesh.CreateWithoutCache();
        //     AssetManager.Meshes.Put("Assets/Virtual/BasicEnemy", mesh);
        // }
        // InstanceManager.Mesh = StaticMesh.CreateFromDisk("Meshes/invader01-crab.obj");
        // InstanceManager.Material =
        //     MaterialBuilder.Begin(typeof(BasicEnemyManager)).SetSamplingTexture(false).Compile();
            // .CreateFromDisk("Meshes/BillboardSprite/BillboardSprite");
            // .Instantiate()
            // .LoadTexture(Texture.CreateFromDisk("Textures/godot.png"));
    }

    private int _enemiesQueued = 0;
    [OnTimer(Seconds = 0.10f)]
    protected void SpawnEnemy()
    {
        if (GetChildren<BasicEnemy>().Count >= 250)
            return;

        var player = ParentScene.Actors.OfType<PlayerCharacter>().FirstOrDefault();
        if (player is null) 
            return;

        _enemiesQueued += 1;
        for (var i = 0; i < _enemiesQueued; i++)
        {
            var transform = Transform.Identity;
            transform.Rotate(0, Random.Shared.NextDouble() * 360, 0);
            transform.TranslateLocal(200, 0, 0);
            transform.TranslateGlobal(player.WorldTransform.Position);
        
            // TODO: Understand why rotation is affected by scale
            // transform.Scale = new Vector3(2,2,2);
            // transform.Rotation = Quaternion.Identity;
            // transform.RotateAroundLocal(Vector3.Pitch, -90);
            transform.Rotation = Quaternion.Identity;
            transform.TranslateGlobal(0, Random.Shared.NextDouble() * 1.0, 0);
            // transform.RotateAroundLocal(Vector3.Right, -2);
            transform.Rescale(1.5, 1.5, 1.5);
            var instance = CreateComponent<BasicEnemy>();
            instance.Transform = transform;
        }
        _enemiesQueued = 0;
    }
    
    [OnTimer(Seconds = 0.10f)]
    protected void OnUpdate(double deltaTime)
    {
        var player = ParentScene.Actors.OfType<PlayerCharacter>().FirstOrDefault();
        if (player is null)
            return;

        foreach (var enemy in GetChildren<BasicEnemy>())
        {
            if (enemy.IsDying)
                continue;
            enemy.Transform.Rotation = enemy.Transform.LookAt(
                player.WorldTransform.Position, Vector3.Up
            ).Rotation;
        }
    }
    
    [OnTimer(Seconds = 0.25f)]
    protected void OnSlowUpdate(double deltaTime)
    {
        var player = ParentScene.Actors.OfType<PlayerCharacter>().FirstOrDefault();
        if (player is null)
            return;

        const double movementSpeed = 15.0;
        foreach (var enemy in GetChildren<BasicEnemy>())
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
            
            enemy.QueueFree();
            _enemiesQueued += 1;
        }
    }
}

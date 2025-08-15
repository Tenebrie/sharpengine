using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Meshes.Builtins;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using User.Game.Player;

namespace User.Game.Actors;

public partial class BasicEnemyManager : Actor
{
    [Component]
    public InstancedActorComponent<BasicEnemy> InstanceManager;
    
    [OnReady]
    protected void OnReady()
    {
        // if (!AssetManager.Meshes.TryGet("Assets/Virtual/BasicEnemy", out var mesh))
        // {
        //     mesh = TessellatedPlaneMesh.CreateWithoutCache();
        //     AssetManager.Meshes.Put("Assets/Virtual/BasicEnemy", mesh);
        // }
        InstanceManager.Mesh = StaticMesh.CreateFromDisk("Meshes/invader01-crab.obj");
        InstanceManager.Material =
            MaterialBuilder.Begin(typeof(BasicEnemyManager)).SetSamplingTexture(false).Compile();
            // .CreateFromDisk("Meshes/BillboardSprite/BillboardSprite");
            // .Instantiate()
            // .LoadTexture(Texture.CreateFromDisk("Textures/godot.png"));
    }

    private int _enemiesQueued = 0;
    [OnTimer(Seconds = 0.05f)]
    protected void SpawnEnemy()
    {
        if (InstanceManager.InstanceCount >= 300)
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
            var instance = InstanceManager.CreateInstance();
            instance.Transform = transform;
        }
        _enemiesQueued = 0;
    }
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        var player = ParentScene.Actors.OfType<PlayerCharacter>().FirstOrDefault();
        if (player is null)
            return;

        const double movementSpeed = 15.0;
        foreach (var enemy in InstanceManager.Instances)
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
            enemy.Transform.Rotation = enemy.Transform.LookAt(
                player.WorldTransform.Position, Vector3.Up
            ).Rotation;
            
            var distanceToPlayer = enemy.Transform.Position.DistanceTo(player.WorldTransform.Position);
            if (distanceToPlayer < 250)
                continue;
            
            enemy.QueueFree();
            _enemiesQueued += 1;
        }
    }
}

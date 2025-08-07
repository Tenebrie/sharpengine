using Engine.Core.Assets;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Meshes.Builtins;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using Engine.Native.Bgfx;
using User.Game.Player;

namespace User.Game.Actors;

public partial class BasicEnemyManager : Actor
{
    [Component]
    public InstancedActorComponent<BasicEnemy> InstanceManager;
    
    [OnReady]
    protected void OnReady()
    {
        if (!AssetManager.Meshes.TryGet("Assets/Virtual/BasicEnemy", out var mesh))
        {
            mesh = TessellatedPlaneMesh.CreateWithoutCache();
            AssetManager.Meshes.Put("Assets/Virtual/BasicEnemy", mesh);
        }
        InstanceManager.Mesh = mesh;
        InstanceManager.Material = Material
            .CreateFromDisk("Meshes/BillboardSprite/BillboardSprite")
            .Instantiate()
            .SetTexture(Texture.CreateFromDisk("Textures/godot.png"));
        InstanceManager.RenderFlags = Bgfx.StateFlags.BlendAlphaToCoverage;
    }

    private int _enemiesQueued = 0;
    [OnTimer(Seconds = 0.02f)]
    protected void SpawnEnemy()
    {
        if (InstanceManager.InstanceCount >= 500)
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
            transform.RotateAroundLocal(Vector3.Right, 2);
            transform.Rescale(5, 5, 5);
            InstanceManager.AddInstance(transform);
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
            enemy.Physics.Velocity =
                (player.WorldTransform.Position - enemy.WorldTransform.Position).NormalizeInPlace() * movementSpeed;
            
            var distanceToPlayer = enemy.Transform.Position.DistanceTo(player.WorldTransform.Position);
            if (distanceToPlayer < 250)
                continue;
            
            enemy.QueueFree();
            _enemiesQueued += 1;
        }
    }
}

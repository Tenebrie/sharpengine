﻿using Engine.Core.Assets;
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
    
    [OnInit]
    protected void OnInit()
    {
        if (!AssetManager.HasMesh("Virtual/BasicEnemy"))
        {
            Console.WriteLine("Creating virtual mesh for BasicEnemy");
            AssetManager.PutMesh("Virtual/BasicEnemy", PlaneMesh.Create());
        }
        InstanceManager.Mesh = AssetManager.LoadMesh("Virtual/BasicEnemy");
        InstanceManager.Material = AssetManager.LoadMaterial("Meshes/HonseTerrain/HonseTerrain");
        InstanceManager.Material.LoadTexture("Assets/Textures/godot.png");
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

        var movementSpeed = 15.0 * deltaTime;
        foreach (var enemy in InstanceManager.Instances)
        {
            enemy.Transform.Position += (player.WorldTransform.Position - enemy.WorldTransform.Position).NormalizeInPlace() * movementSpeed;
            
            var distanceToPlayer = enemy.Transform.Position.DistanceTo(player.WorldTransform.Position);
            if (distanceToPlayer < 2.5)
                enemy.QueueFree();
            if (distanceToPlayer < 250)
                continue;
            
            enemy.QueueFree();
            _enemiesQueued += 1;
        }
    }
}

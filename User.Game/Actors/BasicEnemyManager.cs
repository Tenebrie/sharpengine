using Engine.Assets;
using Engine.Assets.Meshes.Builtins;
using Engine.Core.Common;
using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace User.Game.Actors;

public class BasicEnemyManager : Actor
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
        InstanceManager.BoundingSphere.Generate(PlaneMesh.CreateVerts());
    }
}

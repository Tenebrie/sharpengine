using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Actors;

public partial class BasicEnemy : ActorInstance
{
    // [Component] public StaticMeshComponent Mesh;
    [Component] public PhysicsComponent Physics;

    public double Health { get; set; } = 100.0;

    public void DealDamage(double damage)
    {
        Health -= damage;
        if (Health <= 0)
            QueueFree();
    }

    [OnInit]
    protected void OnInit()
    {
        // if (!AssetManager.HasMesh("Virtual/BasicEnemy"))
        // {
        //     Console.WriteLine("Creating virtual mesh for BasicEnemy");
        //     AssetManager.PutMesh("Virtual/BasicEnemy", PlaneMesh.Create());
        // }
        // Mesh.Mesh = AssetManager.LoadMesh("Virtual/BasicEnemy");
        // Mesh.Material = AssetManager.LoadMaterial("Meshes/HonseTerrain/HonseTerrain");
        // Mesh.Material.LoadTexture("Assets/Textures/godot.png");
        // Mesh.BoundingSphere.Generate(PlaneMesh.CreateVerts());
        // Transform.RotateAroundLocal(Vector3.Pitch, -90);
        // Transform.Rescale(3, 3, 3);
    }
}
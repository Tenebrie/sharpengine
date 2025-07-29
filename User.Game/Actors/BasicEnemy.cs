using Engine.Assets.Materials.Meshes.AlliedProjectile;
using Engine.Assets.Materials.Meshes.HonseTerrain;
using Engine.Assets.Materials.Meshes.RawColor;
using Engine.Assets.Meshes.Builtins;
using Engine.Core.Common;
using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace User.Game.Actors;

public class BasicEnemy : Actor
{
    [Component] public StaticMeshComponent Mesh;

    [OnInit]
    protected void OnInit()
    {
        Mesh.Material = new HonseTerrainMaterial();
        Mesh.Mesh = PlaneMesh.Create();
        Mesh.BoundingSphere.Generate(PlaneMesh.CreateVerts());
        Mesh.Material.LoadTexture("Assets/Textures/godot.png");
        Transform.RotateAroundLocal(Vector3.Pitch, -90);
        Transform.Rescale(3, 3, 3);
    }
}
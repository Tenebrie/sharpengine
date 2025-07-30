using System.Drawing;
using System.Numerics;
using Engine.Assets;
using Engine.Assets.Loaders;
using Engine.Assets.Materials;
using Engine.Assets.Materials.Meshes.HonseTerrain;
using Engine.Assets.Meshes;
using Engine.Assets.Meshes.Builtins;
using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace User.Game.Actors;

public class UnitCube : Actor
{
    [Component]
    public InstancedActorComponent<UnitCubeInstance> InstanceManager;
    
    [OnInit]
    protected void OnInit()
    {
        // InstanceManager.Mesh = new StaticMesh();
        InstanceManager.Material = AssetManager.LoadMaterial("Meshes/RawColor/RawColor");
        CubeMesh.Instance.Load();
        InstanceManager.Mesh = CubeMesh.Instance.Mesh;
        
        AssetVertex[] verts =
        [
            new(new Vector3(-1, -1, -1), Vector2.Zero, Vector3.One, Color.Red),
            new(new Vector3( 1, -1, -1), Vector2.Zero, Vector3.One, Color.Green),
            new(new Vector3( 1,  1, -1), Vector2.Zero, Vector3.One, Color.Yellow),
            new(new Vector3(-1,  1, -1), Vector2.Zero, Vector3.One, Color.Blue),
            new(new Vector3(-1, -1,  1), Vector2.Zero, Vector3.One, Color.Cyan),
            new(new Vector3( 1, -1,  1), Vector2.Zero, Vector3.One, Color.Magenta),
            new(new Vector3( 1,  1,  1), Vector2.Zero, Vector3.One, Color.White),
            new(new Vector3(-1,  1,  1), Vector2.Zero, Vector3.One, Color.Gray)
        ];
        InstanceManager.BoundingSphere.Generate(verts);
        // ObjMeshLoader.LoadObj("bin/decimated_dragon32.obj", out AssetVertex[] vertices, out var indices);
        // InstanceManager.Mesh.Load(vertices, indices);
    } 
}

public class UnitCubeInstance : ActorInstance
{
    // TODO: Performance fix on large number of ticking instances?
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        Transform.Rotate(3 * deltaTime, 5 * deltaTime, 7 * deltaTime);
    }
}
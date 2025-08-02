using System.Drawing;
using System.Numerics;
using Engine.Core.Assets;
using Engine.Core.Assets.Loaders;
using Engine.Core.Assets.Meshes.Builtins;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Actors;

public partial class UnitCube : Actor
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

public partial class UnitCubeInstance : ActorInstance
{
    // TODO: Performance fix on large number of ticking instances?
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        Transform.Rotate(3 * deltaTime, 5 * deltaTime, 7 * deltaTime);
    }
}
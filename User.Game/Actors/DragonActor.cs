using Engine.Assets.Loaders;
using Engine.Assets.Materials;
using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace User.Game.Actors;

public class DragonActor : Actor
{
    [Component] protected StaticMeshComponent MeshComponent;
    
    [OnInit]
    protected void OnInit()
    {
        ObjMeshLoader.LoadObj("bin/decimated_dragon32.obj", out AssetVertex[] vertices, out var indices);
        MeshComponent.Mesh.Load(vertices, indices);
        MeshComponent.Material = new UnlitMaterial();
    }
}
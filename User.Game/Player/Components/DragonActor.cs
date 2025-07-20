using Engine.Assets.Loaders;
using Engine.Assets.Materials;
using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace User.Game.Player.Components;

public class DragonMesh : Actor
{
    [Component] public StaticMeshComponent MeshComponent;
    
    [OnInit]
    protected void OnInit()
    {
        ObjMeshLoader.LoadObj("bin/decimated_dragon32.obj", out var vertices, out var indices);
        MeshComponent.Mesh.Load(vertices, indices);
        MeshComponent.Material = new UnlitMaterial();
    }

}
using Engine.Assets;
using Engine.Core.Makers;
using Engine.Worlds.Attributes;
using Engine.Worlds.Components;
using Engine.Worlds.Entities;

namespace User.Game.Player.Components;

public partial class DragonMesh : Actor
{
    [Component] public StaticMeshComponent MeshComponent;
    
    [OnInit]
    protected void OnInit()
    {
        MeshComponent.Mesh = AssetManager.LoadMesh("Assets/Meshes/decimated_dragon32.obj");
        MeshComponent.Material = AssetManager.LoadMaterial("Meshes/RawColor/RawColor");
        
        MeshComponent.Transform.Rotation = QuatMakers.FromRotation(90, 0, 0);
    }
}
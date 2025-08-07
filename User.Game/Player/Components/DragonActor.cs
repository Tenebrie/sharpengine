using Engine.Core.Assets;
using Engine.Core.Makers;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Player.Components;

public partial class DragonMesh : Actor
{
    [Component] public StaticMeshComponent MeshComponent;
    
    [OnReady]
    protected void OnReady()
    {
        MeshComponent.Mesh = AssetManager.Meshes.LoadFromDisk("Meshes/decimated_dragon32.obj");
        MeshComponent.Material = AssetManager.Materials.Instantiate("Meshes/RawColor/RawColor");
        
        MeshComponent.Transform.Rotation = QuatMakers.FromRotation(90, 0, 0);
    }
}
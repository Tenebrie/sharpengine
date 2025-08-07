using Engine.Core.Assets;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
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
        MeshComponent.Mesh = StaticMesh.CreateFromDisk("Meshes/decimated_dragon32.obj");
        MeshComponent.Material = Material.CreateFromDisk("Meshes/RawColor/RawColor").Instantiate();
        
        MeshComponent.Transform.Rotation = QuatMakers.FromRotation(90, 0, 0);
    }
}
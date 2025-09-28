using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Actors.BasicWall;

public partial class BasicWall : Actor
{
    [Component] public StaticMeshComponent MeshComponent;
    private static MaterialInstance _generatedMaterial;    

    [OnPrepareResources]
    public static void PrepareResources()
    {
        _generatedMaterial = MaterialBuilder.CreateFromDisk("Shaders/cube").WithCache().Instantiate();
    }

    [OnReady]
    public void OnReady()
    {
        MeshComponent.StaticMesh = StaticMesh.CreateFromDisk("Meshes/testwall.obj");
        MeshComponent.MaterialInstance = _generatedMaterial;
    }
}

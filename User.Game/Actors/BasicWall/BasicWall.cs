using System.Drawing;
using Engine.Core.Assets;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;

namespace User.Game.Actors.BasicWall;

public partial class BasicWall : Actor
{
    [Component] public StaticMeshComponent MeshComponent;
    private static MaterialInstance _generatedMaterial;    

    [OnPrepareResources]
    public static void PrepareResources()
    {
        _generatedMaterial = MaterialBuilder.Begin<BasicWall>()
            .SetTintColor(Color.Bisque)
            .SetSamplingTexture(false)
            .Compile()
            .Instantiate();
    }

    [OnReady]
    public void OnReady()
    {
        MeshComponent.Mesh = StaticMesh.CreateFromDisk("Meshes/testwall.obj");
        MeshComponent.MaterialInstance = _generatedMaterial;
    }
}

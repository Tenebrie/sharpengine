using System.Drawing;
using Engine.Core.Assets;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;

namespace User.Game.Actors.BasicWall;

public partial class BasicWall : Actor
{
    [Component] public StaticMeshComponent StaticMesh;
    private static Material _generatedMaterial;

    [OnPrepareResources]
    public static void PrepareResources()
    {
        var material = MaterialBuilder.Begin(typeof(BasicWall))
            .SetTintColor(Color.Bisque)
            .SetSamplingTexture(false)
            .Compile();
        _generatedMaterial = material;
    }

    [OnReady]
    public void OnReady()
    {
        StaticMesh.Mesh = AssetManager.LoadMesh("Assets/Meshes/testwall.obj");
        StaticMesh.Material = AssetManager.InstantiateMaterial(_generatedMaterial);
    } 
}

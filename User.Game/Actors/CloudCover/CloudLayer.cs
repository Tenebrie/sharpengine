using System.Drawing;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Actors.CloudCover;

public partial class CloudLayer : Actor
{
    [Component] protected StaticMeshComponent MeshComponent;

    [OnReady]
    protected void OnReady()
    {
        MeshComponent.Mesh = PlaneMesh.Shared;
        MeshComponent.MaterialInstance = Material.CreateFromDisk("Assets/Shaders/Meshes/Clouds")
            .Instantiate()
            .SetSamplingTexture(false)
            .SetTintColor(Color.PaleGoldenrod);
    }
}

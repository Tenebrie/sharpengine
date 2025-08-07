using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Player.Abilities.Definitions;

public partial class LightningStrikeEffect : Actor
{
    [Component] public StaticMeshComponent MeshComponent;
    private static MaterialInstance _materialInstance;

    [OnPrepareResources]
    public static void OnPrepareResources()
    {
        _materialInstance = MaterialBuilder.Begin(typeof(LightningStrikeEffect))
            .SetSamplingTexture(false)
            .Compile()
            .Instantiate()
            .SetTexture(Texture.CreateFromDisk("Textures/lightning.png"));
    }

    [OnReady]
    public void OnReady()
    {
        MeshComponent.Mesh = PlaneMesh.Shared;
        MeshComponent.Material = _materialInstance;  
    }
}
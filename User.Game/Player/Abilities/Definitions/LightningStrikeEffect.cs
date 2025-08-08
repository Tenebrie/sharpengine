using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using Engine.Native.Bgfx;

namespace User.Game.Player.Abilities.Definitions;

public partial class LightningStrikeEffect : Actor
{
    [Component] public StaticMeshComponent MeshComponent;
    private static MaterialInstance _materialInstance;

    [OnPrepareResources]
    public static void OnPrepareResources()
    {
        _materialInstance = MaterialBuilder.Begin(typeof(LightningStrikeEffect))
            .SetSamplingTexture(true)
            .Compile()
            .Instantiate()
            .LoadTexture(Texture.CreateFromDisk("Textures/lightning.png"));
    }

    [OnReady]
    public void OnReady()
    {
        MeshComponent.Mesh = PlaneMesh.Shared;
        MeshComponent.Material = _materialInstance;
        MeshComponent.Transform.Position = new Vector3(-0, 0, -0.5 + 0.1);
        MeshComponent.RenderFlags = Bgfx.StateFlags.BlendAlphaToCoverage;
    }

    [OnTimer(Seconds = 0.15f)]
    public void OnDestroy()
    {
        QueueFree();
    }
}
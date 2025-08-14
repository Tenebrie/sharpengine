using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Player.Abilities.Definitions;

public partial class LightningStrikeEffect : Actor
{
    [Component] public StaticMeshComponent MeshComponent;
    private static Material _material;
    private MaterialInstance _materialInstance = null!;
    
    [OnPrepareResources]
    public static void OnPrepareResources()
    {
        _material = MaterialBuilder.Begin(typeof(LightningStrikeEffect))
            .SetSamplingTexture(true)
            .Compile();
    }

    [OnReady]
    public void OnReady()
    {
        _materialInstance = _material.Instantiate()
            .LoadTexture(Texture.CreateFromDisk("Textures/lightning.png"));
        MeshComponent.Mesh = PlaneMesh.Shared;
        MeshComponent.MaterialInstance = _materialInstance;
        MeshComponent.Transform.Position = new Vector3(-0, 0, -0.5 + 0.1);
    }

    private bool _isFading = false;
    private double _opacity = 1.0;
    [OnUpdate]
    public void OnUpdate(double deltaTime)
    {
        if (!_isFading)
            return;
        _opacity -= deltaTime * 3.0;
        _materialInstance.SetOpacity(_opacity);
    }
    
    [OnTimer(Seconds = 0.15f, TicksOnce = true)]
    public void OnStartFade()
    {
        _isFading = true;
    }

    [OnTimer(Seconds = 0.45f)]
    public void OnDestroy()
    {
        QueueFree();
    }
}
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Actors.BasicEnemies;

public partial class SpaceshipEngineComponent : ActorComponent
{
    [Component] protected StaticMeshComponent Mesh;
    
    private int _animationFrame = 0;

    public void BumpAnimation()
    {
        _animationFrame += 1;
    }

    [OnReady]
    protected void OnReady()
    {
        Mesh.StaticMesh = PlaneMesh.Shared;
        Mesh.MaterialInstance = MaterialBuilder.BeginFromFilesystem("Shaders/cube")
            .SetTexture(Texture.CreateFromDisk("Textures/spaceship-flame.png"))
            .Instantiate()
            .SetUvOffset(new Vector2(0, 0))
            .SetUvScale(new Vector2(1, 1));
    }

    [OnTimer(Seconds = 0.10)]
    protected void OnUpdate()
    {
        _animationFrame = (_animationFrame + 1) % 4;
        switch (_animationFrame)
        {
            case 0:
                Mesh.MaterialInstance.SetUvOffset(new Vector2(0.0, 0));
                break;
            case 2:
                Mesh.MaterialInstance.SetUvOffset(new Vector2(0.215, 0));
                break;
            case 1:
                Mesh.MaterialInstance.SetUvOffset(new Vector2(0.465, 0));
                break;
            default:
                Mesh.MaterialInstance.SetUvOffset(new Vector2(0.765, 0));
                break;
        }

        Mesh.MaterialInstance.SetUvScale(new Vector2(0.25, 1));
    }
}
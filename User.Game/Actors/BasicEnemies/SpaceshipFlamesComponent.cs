using Engine.Core.Assets.Materials;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Actors.BasicEnemies;

public partial class SpaceshipFlamesComponent : ActorInstance
{
    private int _animationFrame = 0;

    public void BumpAnimation()
    {
        _animationFrame += 1;
    }

    [OnReady]
    protected void OnReady()
    {
        MaterialInstance.LoadTexture(Texture.CreateFromDisk("Textures/spaceship-flame.png"))
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
                MaterialInstance.SetUvOffset(new Vector2(0.0, 0));
                break;
            case 2:
                MaterialInstance.SetUvOffset(new Vector2(0.215, 0));
                break;
            case 1:
                MaterialInstance.SetUvOffset(new Vector2(0.465, 0));
                break;
            default:
                MaterialInstance.SetUvOffset(new Vector2(0.765, 0));
                break;
        }

        MaterialInstance.SetUvScale(new Vector2(0.25, 1));
    }
}
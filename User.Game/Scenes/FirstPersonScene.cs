using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Input.Attributes;
using User.Game.FirstPerson;

namespace User.Game.Scenes;

public partial class FirstPersonScene : Scene
{
    [OnReady]
    protected void OnReady()
    {
        CreateActor<FirstPersonPlayer>();
        var honseTerrain = CreateActor<ShardTerrain>();
        honseTerrain.Transform.Position = (0, -2, 0);
        const int scale = 10;
        // honseTerrain.Transform.RotateAroundGlobal(Vector3.Up, 45);
        honseTerrain.Transform.Scale = (scale, scale, scale);
    }
}
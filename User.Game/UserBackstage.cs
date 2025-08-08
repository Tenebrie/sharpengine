using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using User.Game.Actors;
using User.Game.Actors.BasicWall;
using User.Game.Player;
using User.Game.Services;

namespace User.Game;

[MainBackstage]
public partial class UserBackstage : Backstage
{
    [OnReady]
    protected void OnReady()
    {
        var scene = new UserScene();
        AdoptChild(scene);
    }
}

public partial class UserScene : Scene
{
    [OnReady]
    protected void OnReady()
    {
        RegisterService<UserInputService>();
        
        var player = CreateActor<PlayerCharacter>();
        var cameraFollower = CreateActor<PlayerCameraFollower>();
        cameraFollower.PlayerCharacter = player;
        
        var honseTerrain = CreateActor<HonseTerrain>();
        honseTerrain.Transform.Position = new Vector3(0, -0.05, 0);
        const int scale = 30;
        honseTerrain.Transform.Scale = new Vector3(scale, scale, scale);

        var wall = CreateActor<BasicWall>();
        wall.Transform.Position = new Vector3(0, 0, -20);

        CreateActor<BasicEnemyManager>();
    }
}

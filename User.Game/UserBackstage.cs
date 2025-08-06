using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;
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
        
        // var terrain = CreateActor<GameTerrain>();
        // terrain.Transform.Position = new Vector(0, -10, 0);
        // var scale = 250.0;
        // terrain.Transform.Scale = new Vector(scale, 250.0, scale);
        
        var player = CreateActor<PlayerCharacter>();
        var cameraFollower = CreateActor<PlayerCameraFollower>();
        cameraFollower.PlayerCharacter = player;
        //
        // var basicEnemy = CreateActor<BasicEnemy>();
        // basicEnemy.Transform.Position = new Vector3(0, 0, -20);
        //
        var honseTerrain = CreateActor<HonseTerrain>();
        honseTerrain.Transform.Position = new Vector3(0, -3.25, 0);
        const int scale = 30;
        honseTerrain.Transform.Scale = new Vector3(scale, scale, scale);

        var wall = CreateActor<BasicWall>();
        wall.Transform.Position = new Vector3(0, 0, -20);

        CreateActor<BasicEnemyManager>();
    }
}

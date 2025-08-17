using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using User.Game.Actors;
using User.Game.Actors.BasicEnemies;
using User.Game.Actors.BasicWall;
using User.Game.Actors.CloudCover;
using User.Game.Player;
using User.Game.Services;

namespace User.Game;

[MainBackstage]
public partial class UserBackstage : Backstage
{
    [OnReady]
    protected void OnReady()
    {
        CreateScene<UserScene>();
    }
}

public partial class BasicEnemyScene : Scene
{
    [OnReady]
    protected void OnReady()
    {
        var manager = CreateActor<BasicEnemyManager>();
        manager.InstanceManager.CreateInstance();
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
        honseTerrain.Transform.Position = new Vector3(-100, -5000, -2000);
        const int scale = 700;
        honseTerrain.Transform.RotateAroundGlobal(Vector3.Up, 45);
        honseTerrain.Transform.Scale = new Vector3(scale, scale, scale);

        CreateActor<BasicEnemyManager>();
        var cloudLayer = CreateActor<CloudLayer>();
        cloudLayer.Transform.Position = new Vector3(0, -1000, 0);
        const int cloudScale = 7000;
        cloudLayer.Transform.Scale = new Vector3(cloudScale, 1, cloudScale);
        
        cloudLayer = CreateActor<CloudLayer>();
        cloudLayer.Transform.Position = new Vector3(1000, -2000, 500);
        cloudLayer.Transform.Scale = new Vector3(cloudScale * 1.5, 1, cloudScale * 1.5);
        cloudLayer.Transform.RotateAroundLocal(Vector3.Up, 135);
        
        cloudLayer = CreateActor<CloudLayer>();
        cloudLayer.Transform.Position = new Vector3(2000, -3000, 1000);
        cloudLayer.Transform.Scale = new Vector3(cloudScale * 2, 1, cloudScale * 2);
        cloudLayer.Transform.RotateAroundLocal(Vector3.Up, 250);
    }
}

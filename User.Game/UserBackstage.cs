using Engine.Core.Common;
using Engine.Core.Makers;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using User.Game.Actors;
using User.Game.Player;
using User.Game.Services;

namespace User.Game;

[MainBackstage]
public class UserBackstage : Backstage
{
    [OnInit]
    protected void OnInit()
    {
        var scene = new UserScene();
        AdoptChild(scene);
    }
}

public class UserScene : Scene
{
    [OnInit]
    protected void OnInit()
    {
        RegisterService<UserInputService>();
        
        // var terrain = CreateActor<GameTerrain>();
        // terrain.Transform.Position = new Vector(0, -10, 0);
        // var scale = 250.0;
        // terrain.Transform.Scale = new Vector(scale, 250.0, scale);
        var player = CreateActor<PlayerCharacter>();
        var cameraFollower = CreateActor<PlayerCameraFollower>();
        cameraFollower.PlayerCharacter = player;

        var honseTerrain = CreateActor<HonseTerrain>();
        honseTerrain.Transform.Position = new Vector(0, -3.25, 0);
        
        var cubeManager = CreateActor<UnitCube>();
        for (var x = 0; x < 5; x++)
        {
            for (var y = 0; y < 5; y++)
            {  
                var transform = Transform.Identity; 
                transform.Position = new Vector(x * 2, y * 2 - 20, -10);
                transform.Scale = new Vector(0.4f, 0.4f, 0.4f);
                transform.Rotation = Quaternion.Identity;
                cubeManager.InstanceManager.AddInstance(transform);
            }
        }
    }
}

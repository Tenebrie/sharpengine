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
        //
        // var basicEnemy = CreateActor<BasicEnemy>();
        // basicEnemy.Transform.Position = new Vector3(0, 0, -20);
        //
        var honseTerrain = CreateActor<HonseTerrain>();
        honseTerrain.Transform.Position = new Vector3(0, -3.25, 0);
        var scale = 30;
        honseTerrain.Transform.Scale = new Vector3(scale, scale, scale);
        //
        var cubeManager = CreateActor<UnitCube>();
        for (var x = 0; x < 5; x++)
        {
            for (var y = 0; y < 5; y++)
            {  
                var transform = Transform.Identity;
                transform.Position = new Vector3(x * 2, y * 2 + 20, -10);
                transform.Scale = new Vector3(0.4f, 0.4f, 0.4f);
                transform.Rotation = Quaternion.Identity;
                cubeManager.InstanceManager.AddInstance(transform);
            }
        }
        
        var enemyManager = CreateActor<BasicEnemyManager>();
        for (var x = -25; x < 26; x++)
        {
            for (var y = -25; y < 26; y++)
            {
                // var enemy = CreateActor<BasicEnemy>();
                var transform = Transform.Identity;
                transform.Position = new Vector3(x * 10, 3, y * 10);
                transform.Scale = new Vector3(0.4f, 0.4f, 0.4f);
                transform.Rotation = Quaternion.Identity;
                // enemy.Transform = transform;
                enemyManager.InstanceManager.AddInstance(transform);
            }
        }
    }
}

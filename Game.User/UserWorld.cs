using Engine.Worlds;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Game.User.Actors;
using JetBrains.Annotations;

namespace Game.User;

[MainBackstage]
public class UserBackstage : Backstage
{
    [OnInit]
    protected void OnInit()
    {
        var scene = new UserScene();
        RegisterChild(scene);
    }
}

public class UserScene : Scene
{
    [OnInit]
    protected void OnInit()
    {
        Actor actor = CreateActor<UserActor>();
        actor.Transform.Position = new Vector(15, 15, 50);
        for (var x = 0; x < 15; x++)
        {
            for (var y = 0; y < 15; y++)
            {
                var cube = CreateActor<UnitCube>();
                cube.Transform.Position = new Vector(x * 2, y * 2 - 8, -10);
                cube.Transform.Scale = new Vector(0.4f, 0.4f, 0.4f);
                cube.Transform.Rotation = Quaternion.Identity;
            }
        }
    }
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
    }
}

public class UserActor : Actor
{
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        
    }
}
using Engine.Core.Common;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using User.Game.Actors;

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
        CreateActor<MainCamera>();
        // Actor actor = CreateActor<UserActor>();
        // actor.Transform.Position = new Vector(15, 15, 50);
        var cubeManager = CreateActor<UnitCube>();
        for (var x = 0; x < 50; x++)
        {
            for (var y = 0; y < 50; y++)
            {
                var transform = Transform.Identity;
                transform.Position = new Vector(x * 2, y * 2 - 8, -10);
                transform.Scale = new Vector(0.4f, 0.4f, 0.4f);
                transform.Rotation = Quaternion.Identity;
                cubeManager.InstanceManager.AddInstance(transform);
            }
        }
    }
}

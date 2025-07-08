using Engine.Worlds;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
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
        var actor = CreateActor<UserActor>();
        actor.Transform.Position = new Vector(15, 15, 50);
        Console.WriteLine("UserScene initialized with UserActor.");
    }
}

public class UserActor : Actor
{
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        if (Transform.Position.X < 30)
        {
            Transform.Position += new Vector(10, 0, 0) * deltaTime;
            Console.WriteLine($"UserActor walked to {Transform.Position}.");
        }

        if (Transform.Position.X >= 30)
        {
            Console.WriteLine($"Destroying UserActor at position {Transform.Position}." + Backstage);
            QueueFree();
        }
    }
}
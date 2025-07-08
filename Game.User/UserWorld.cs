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
        var random = new Random();
        actor = CreateActor<BouncingLogo>();
        actor.Transform.Position = new Vector(random.NextDouble() * 800, random.NextDouble() * 600, 0);
    }
}

public class UserActor : Actor
{
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        
    }
}
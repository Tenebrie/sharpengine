using Engine.Worlds;
using Engine.Worlds.Entities;

namespace Game.User;

public class UserWorld : World
{
    protected override void OnInit()
    {
        Units.Add(new UserUnit());
    }
}

public class UserUnit : Unit
{
    protected override void OnUpdate(double deltaTime)
    {
        base.OnUpdate(deltaTime);
        Console.WriteLine("UserUnit is updating with deltaTime: " + deltaTime);
    }
}
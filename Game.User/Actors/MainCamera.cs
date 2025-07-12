using Engine.Input.Attributes;
using Engine.Worlds.Entities.BuiltIns;
using Silk.NET.Input;

namespace Game.User.Actors;

public class MainCamera : Camera
{
    [OnInputActionHeld(Key.W, 1.0f)]
    [OnInputActionHeld(Key.S, -1.0f)]
    protected void OnForward(double deltaTime, double value)
    {
        Console.WriteLine(Transform.Position);
        Transform.Translate(0, value * deltaTime, 0);
    }
    
    [OnInputActionHeld(Key.D, 1.0f)]
    [OnInputActionHeld(Key.A, -1.0f)]
    protected void OnSideways(double deltaTime, double value)
    {
        Console.WriteLine(Transform.Position);
        Transform.Translate(value * deltaTime, 0, 0);
    }
}

using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace User.Game.Actors;



public class MainCamera : ActorComponent
{
    [Component] public MainCameraImpl Camera;
}

public class MainCameraImpl : Camera
{
    
}

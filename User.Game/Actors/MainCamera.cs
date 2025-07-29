using Engine.Core.Common;
using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace User.Game.Actors;

public class MainCamera : ActorComponent
{
    [Component] public MainCameraImpl Camera;
    
    [OnInit]
    protected void OnInit()
    {
        Camera.Transform.Position = new Vector3(0, -30, 0);
    }
}

public class MainCameraImpl : Camera
{
    
}

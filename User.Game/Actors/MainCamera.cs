using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;

namespace User.Game.Actors;

public partial class MainCamera : ActorComponent
{
    [Component] public MainCameraImpl Camera;
    
    [OnInit]
    protected void OnInit()
    {
        Camera.Transform.Position = new Vector3(0, -30, 0);
    }
}

public partial class MainCameraImpl : Camera
{
    
}

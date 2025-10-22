using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;

namespace User.Game.Actors;

public partial class MainCamera : ActorComponent
{
    [Component] public MainCameraImpl Camera;
    
    [OnReady]
    protected void OnReady()
    {
    }
    
    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        TargetOffset += -TargetOffset * deltaTime * 10.0;
        Transform.Position += (-Transform.Position + TargetOffset) * 5.0 * deltaTime;
    }

    public Vector3 TargetOffset = Vector3.Zero;
}

public partial class MainCameraImpl : Camera
{
    
}

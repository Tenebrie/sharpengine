using Engine.Core.Logging;
using Engine.Worlds.Attributes;
using Engine.Worlds.Components;

namespace Engine.Worlds.Entities;

public class ActorInstance : Actor
{
    public bool IsOnScreen { get; set; }

    // [Parent]
    public IInstancedActorComponent ParentManager;

    [OnDestroy]
    protected void NotifyParentOnDestroy()
    {
        ParentManager.RemoveInstance(this);
    }
}
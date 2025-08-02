using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;

namespace Engine.Core.EntitySystem.Entities;

public partial class ActorInstance : Actor
{
    public bool IsOnScreen { get; set; }

    // [Parent]
    public IInstancedActorComponent ParentManager = null!;

    [OnDestroy]
    protected void NotifyParentOnDestroy()
    {
        ParentManager.RemoveInstance(this);
    }
}
using Engine.Core.Assets.Materials;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;

namespace Engine.Core.EntitySystem.Entities;

public partial class ActorInstance : Actor
{
    public bool IsOnScreen { get; set; }

    // [Parent]
    public IInstancedActorComponent? ParentManager;
    public MaterialInstance MaterialInstance = null!;

    [OnDestroy]
    protected void NotifyParentOnDestroy()
    {
        ParentManager?.DestroyInstance(this);
    }
}
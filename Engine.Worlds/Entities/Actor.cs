using Engine.Core.Common;

namespace Engine.Worlds.Entities;

public abstract class Actor : Spatial
{
    protected T CreateComponent<T>() where T : ActorComponent, new()
    {
        return AdoptChild(new T());
    }
}
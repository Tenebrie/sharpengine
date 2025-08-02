using Engine.Core.EntitySystem.Entities;

namespace Engine.Core.EntitySystem.Modules;

public interface IPhysicsModule
{
    public void Register(Atom atom);
    public void Unregister(Atom atom);
    public void Initialize();
}
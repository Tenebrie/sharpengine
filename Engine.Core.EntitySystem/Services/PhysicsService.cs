using Engine.Core.EntitySystem.Entities;

namespace Engine.Core.EntitySystem.Services;

public partial class PhysicsService : Service
{
    public void Register(Atom atom) => Backstage.PhysicsModule?.Register(atom);
    public void Unregister(Atom atom) => Backstage.PhysicsModule?.Unregister(atom);
}
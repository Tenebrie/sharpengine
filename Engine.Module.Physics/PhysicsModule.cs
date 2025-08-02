using Engine.Core.Logging;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Modules;
using JetBrains.Annotations;

namespace Engine.Module.Physics;

[UsedImplicitly]
public class PhysicsModule : IPhysicsModule
{
    public void Register(Atom atom)
    {
        Logger.Info("Registering an atom");
    }

    public void Unregister(Atom atom)
    {
        Logger.Info("Unregistering an atom");
    }

    public void Initialize()
    {
        Logger.Info("Init physics server");
    }
}

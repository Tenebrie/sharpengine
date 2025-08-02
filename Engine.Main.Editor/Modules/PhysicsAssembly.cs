using Engine.Core.EntitySystem.Modules;
using Engine.Main.Editor.Modules.Abstract;

namespace Engine.Hypervisor.Editor.Modules;

internal class PhysicsAssembly(string assemblyName = "Engine.Module.Physics") : GuestAssembly(assemblyName)
{
    internal IPhysicsModule? PhysicsModule { get; private set; }

    public override void Init()
    {
        base.Init();
        PhysicsModule = Host.Load<IPhysicsModule>();
        if (PhysicsModule == null)
        {
            Console.Error.WriteLine("Failed to instantiate physics module.");
            return;
        }
        PhysicsModule.Initialize();
    }
}

using Engine.Core.EntitySystem.Modules;
using Engine.Main.Editor.Modules.Abstract;

namespace Engine.Main.Editor.Modules;

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
    
    public override bool Update(double deltaTime)
    {
        PhysicsModule?.ProcessPhysicsFrame(deltaTime);
        return base.Update(deltaTime);
    }
}

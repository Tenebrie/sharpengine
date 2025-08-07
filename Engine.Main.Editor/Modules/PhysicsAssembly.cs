using Engine.Core.EntitySystem.Modules;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Abstract;

namespace Engine.Main.Editor.Modules;

internal class PhysicsAssembly() : GuestAssembly("Engine.Module.Physics", EngineModule.Physics)
{
    internal IPhysicsModule? PhysicsModule { get; private set; }
    
    internal override bool IgnoresTimeScale => false;

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
    
    public override void Destroy()
    {
        PhysicsModule!.Shutdown();
        base.Destroy();
    }
}

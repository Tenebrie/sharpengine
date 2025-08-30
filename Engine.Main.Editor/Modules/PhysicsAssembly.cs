using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Abstract;

namespace Engine.Main.Editor.Modules;

internal class PhysicsAssembly() : ModularAssembly("Engine.Module.Physics", EngineModule.Physics)
{
    internal IPhysicsHost? PhysicsModule { get; private set; }
    
    internal override IModularHost? GetHost() => PhysicsModule;
    internal override int ImplicitReloadPriority => 1;
    
    public override void Load()
    {
        base.Load();
        PhysicsModule = Loader.ProduceContract<IPhysicsHost>();
        if (PhysicsModule == null)
        {
            Logger.Error("PhysicsAssembly: Failed to instantiate the host.");
            return;
        }
        PhysicsModule.Hypervisor = Editor.Hypervisor.Instance;
        PhysicsModule.Initialize();
    }
    
    public override bool Update(double deltaTime)
    {
        PhysicsModule?.ProcessPhysicsFrame(deltaTime);
        return base.Update(deltaTime);
    }

    public override void Unload()
    {
        PhysicsModule?.Shutdown();
        PhysicsModule = null;
        base.Unload();
    }
}

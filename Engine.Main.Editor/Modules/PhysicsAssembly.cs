using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Abstract;
using Engine.Main.Shared;

namespace Engine.Main.Editor.Modules;

internal class PhysicsAssembly(IEntryPoint entryPoint) : ModularAssembly("Engine.Module.Physics", EngineModule.Physics)
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
        PhysicsModule.Hypervisor = entryPoint.Hypervisor;
        PhysicsModule.Initialize();
    }
    
    public override void Update(double deltaTime)
    {
        try
        {
            PhysicsModule?.ProcessPhysicsFrame(deltaTime * TimeScale);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error during Physics update: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
        }
        base.Update(deltaTime);
    }

    public override void Unload()
    {
        PhysicsModule?.Shutdown();
        PhysicsModule = null;
        base.Unload();
    }
}

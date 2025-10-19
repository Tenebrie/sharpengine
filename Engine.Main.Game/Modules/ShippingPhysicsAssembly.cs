using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Game.Modules.Abstract;
using Engine.Main.Shared;
using Engine.Module.Physics;

namespace Engine.Main.Game.Modules;

internal sealed class ShippingPhysicsAssembly(IEntryPoint entryPoint) : BundledAssembly("Engine.Module.Physics", EngineModule.Physics)
{
    internal PhysicsHost PhysicsModule { get; private set; } = null!;
    
    internal override IModularHost GetHost() => PhysicsModule;
    
    internal override void Load()
    {
        base.Load();
        PhysicsModule = new PhysicsHost
        {
            Hypervisor = entryPoint.Hypervisor
        };
        PhysicsModule.Initialize();
    }
    
    public override void Update(double deltaTime)
    {
        try
        {
            PhysicsModule.ProcessPhysicsFrame(deltaTime * TimeScale);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error during Physics update: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
        }
    }

    internal void Destroy()
    {
        PhysicsModule.Shutdown();
    }
}

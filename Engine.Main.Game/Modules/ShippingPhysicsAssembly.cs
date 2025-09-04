using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Game.Modules.Abstract;
using Engine.Module.Physics;

namespace Engine.Main.Game.Modules;

internal class ShippingPhysicsAssembly() : BundledAssembly("Engine.Module.Physics", EngineModule.Physics)
{
    internal PhysicsHost PhysicsModule { get; private set; } = null!;
    
    internal override IModularHost GetHost() => PhysicsModule;
    
    internal override void Load()
    {
        PhysicsModule = new PhysicsHost
        {
            Hypervisor = Game.Hypervisor.Instance
        };
        PhysicsModule.Initialize();
    }
    
    internal override void Update(double deltaTime)
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

    internal override void Destroy()
    {
        PhysicsModule.Shutdown();
    }
}

using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Game.Modules.Abstract;
using Engine.Module.Utility;

namespace Engine.Main.Game.Modules;

public class ShippingUtilityAssembly() : BundledAssembly("Engine.Module.Utility", EngineModule.Workspace)
{
    internal UtilityHost HostBackstage { get; private set; } = null!;
    
    internal override IModularHost GetHost() => HostBackstage;

    internal override void Load()
    {
        HostBackstage = new UtilityHost();
        HostBackstage.Name = "util-" + Guid.NewGuid();
        HostBackstage.Hypervisor = Game.Hypervisor.Instance;
        HostBackstage.StartupInitialize();
    }

    internal override void Update(double deltaTime)
    {
        try
        {
            HostBackstage.TriggerLogicFrameUpdate(deltaTime * TimeScale);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error during OnUpdate: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
        }
    }

    internal override void Destroy()
    {
        HostBackstage.FreeImmediately();
    }
}

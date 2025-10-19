using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Game.Modules.Abstract;
using Engine.Main.Shared;
using Engine.Module.Utility;

namespace Engine.Main.Game.Modules;

public sealed class ShippingUtilityAssembly(IEntryPoint entryPoint) : BundledAssembly("Engine.Module.Utility", EngineModule.Workspace)
{
    internal UtilityHost HostBackstage { get; private set; } = null!;
    
    internal override IModularHost GetHost() => HostBackstage;

    internal override void Load()
    {
        base.Load();
        HostBackstage = new UtilityHost
        {
            Name = "util-" + Guid.NewGuid(),
            Hypervisor = entryPoint.Hypervisor
        };
        LaminaDiscoveryManager.RegisterLaminaRenderers(Assembly);
        HostBackstage.StartupInitialize();
    }

    public override void Update(double deltaTime)
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

    internal void Destroy()
    {
        HostBackstage.FreeImmediately();
    }
}

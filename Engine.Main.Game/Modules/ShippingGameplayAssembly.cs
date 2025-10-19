using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Game.Modules.Abstract;
using Engine.Main.Shared;

namespace Engine.Main.Game.Modules;

public sealed class ShippingGameplayAssembly(IEntryPoint entryPoint) : BundledAssembly("User.Game", EngineModule.Gameplay)
{
    internal IGameplayHost HostBackstage { get; private set; } = null!;
    private IBaseEngineContract? Contract { get; set; }
    
    internal override IModularHost GetHost() => HostBackstage;

    internal override void Load()
    {
        base.Load();
        Contract = ProduceContract<IBaseEngineContract>();
        HostBackstage = (IGameplayHost)Activator.CreateInstance(Contract.MainBackstage)!;
        HostBackstage.Name = "guest-" + Guid.NewGuid();
        HostBackstage.Hypervisor = entryPoint.Hypervisor;
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
            Logger.Error($"Error during Backstage update: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
        }
    }

    internal void Destroy()
    {
        HostBackstage.FreeImmediately();
    }
}
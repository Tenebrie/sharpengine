using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Game.Modules.Abstract;

namespace Engine.Main.Game.Modules;

public class ShippingGameplayAssembly() : BundledAssembly("User.Game", EngineModule.Gameplay)
{
    internal IGameplayHost HostBackstage { get; private set; } = null!;
    private IBaseEngineContract? Contract { get; set; }
    
    internal override IModularHost GetHost() => HostBackstage;

    internal override void Load()
    {
        Contract = ProduceContract<IBaseEngineContract>();
        HostBackstage = (IGameplayHost)Activator.CreateInstance(Contract.MainBackstage)!;
        HostBackstage.Name = "guest-" + Guid.NewGuid();
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
            Logger.Error($"Error during Backstage update: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
        }
    }

    internal override void Destroy()
    {
        HostBackstage.FreeImmediately();
    }
}
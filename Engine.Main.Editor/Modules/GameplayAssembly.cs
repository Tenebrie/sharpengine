using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Abstract;
using Engine.Main.Shared;

namespace Engine.Main.Editor.Modules;

public class GameplayAssembly(IEntryPoint entryPoint) : ModularAssembly("User.Game", EngineModule.Gameplay)
{
    internal IGameplayHost? HostBackstage { get; private set; }
    private IBaseEngineContract? Contract { get; set; }
    
    internal override IModularHost? GetHost() => HostBackstage;

    public override void Load()
    {
        base.Load();
        Contract = Loader.ProduceContract<IBaseEngineContract>();
        if (Contract == null)
        {
            Logger.Error("GameplayAssembly: Failed to load entry point settings.");
            return;
        }
        HostBackstage = (IGameplayHost)Activator.CreateInstance(Contract.MainBackstage)!;
        HostBackstage.Name = "guest-" + Guid.NewGuid();
        HostBackstage.Hypervisor = entryPoint.Hypervisor;
        
        HostBackstage.StartupInitialize();
        // RegisterLaminaRenderers();
    }

    public override void Update(double deltaTime)
    {
        if (HostBackstage == null)
        {
            base.Update(deltaTime);
            return;
        }

        try
        {
            HostBackstage.TriggerLogicFrameUpdate(deltaTime * TimeScale);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error during Backstage update: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
        }
        base.Update(deltaTime);
    }

    public override void Unload()
    {
        UnregisterLaminaRenderers();
        HostBackstage?.FreeImmediately();
        HostBackstage = null;
        base.Unload();
    }
}
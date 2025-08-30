using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Abstract;

namespace Engine.Main.Editor.Modules;

public class GameplayAssembly() : ModularAssembly("User.Game", EngineModule.Gameplay)
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
        HostBackstage.Hypervisor = Editor.Hypervisor.Instance;
        HostBackstage.StartupInitialize();
    }

    public override bool Update(double deltaTime)
    {
        if (HostBackstage == null)
            return base.Update(deltaTime);

        try
        {
            HostBackstage.TriggerLogicFrameUpdate(deltaTime * TimeScale);
        }
        catch (Exception ex)
        {
            Logger.Error($"Error during Backstage update: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return false;
        }
        return base.Update(deltaTime);
    }

    public override void Unload()
    {
        HostBackstage?.FreeImmediately();
        HostBackstage = null;
        base.Unload();
    }
}
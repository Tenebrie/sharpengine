using Engine.Core.Contracts;
using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Abstract;

namespace Engine.Main.Editor.Modules;

public class GameplayAssembly() : ModularAssembly("User.Game", EngineModule.Gameplay)
{
    private double _updatesPausedFor = 0.0;
    internal IGameplayHost? HostBackstage { get; private set; }
    private IBaseEngineContract? Contract { get; set; }
    
    internal override IModularHost? GetHost() => HostBackstage;

    public override void Load()
    {
        base.Load();
        Contract = Loader.LoadAssembly<IBaseEngineContract>();
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
        if (_updatesPausedFor > 0.0)
        {
            _updatesPausedFor -= deltaTime;
            return base.Update(deltaTime);
        }

        if (HostBackstage == null)
            return base.Update(deltaTime);

        try
        {
            HostBackstage.TriggerLogicFrameUpdate(deltaTime);
            Logger.ClearPersistent("UserGameUpdatesSuppressed");
        }
        catch (Exception ex)
        {
            Logger.Error($"Error during Backstage update: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            Logger.ShowPersistent("UserGameUpdatesSuppressed", "Game updates temporarily suppressed.");
            _updatesPausedFor = 3.0;
            return false;
        }
        return base.Update(deltaTime);
    }

    protected override void Unload()
    {
        HostBackstage?.FreeImmediately();
        base.Unload();
    }
}
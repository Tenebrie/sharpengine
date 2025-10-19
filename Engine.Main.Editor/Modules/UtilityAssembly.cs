using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Abstract;
using Engine.Main.Shared;

namespace Engine.Main.Editor.Modules;

public class UtilityAssembly(IEntryPoint entryPoint) : ModularAssembly("Engine.Module.Utility", EngineModule.Workspace)
{
    internal IUtilityHost? HostBackstage { get; private set; }
    private IBaseEngineContract? Contract { get; set; }
    
    internal override IModularHost? GetHost() => HostBackstage;
    internal override int ImplicitReloadPriority => 1;

    public override void Load()
    {
        base.Load();
        Contract = Loader.ProduceContract<IBaseEngineContract>();
        if (Contract == null)
        {
            Logger.Error("UtilityAssembly: Failed to load entry point settings.");
            return;
        }
        HostBackstage = (IUtilityHost)Activator.CreateInstance(Contract.MainBackstage)!;
        HostBackstage.Name = "util-" + Guid.NewGuid();
        HostBackstage.Hypervisor = entryPoint.Hypervisor;
        HostBackstage.StartupInitialize();
        RegisterLaminaRenderers();
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
            Logger.Error($"[UtilityAssembly] Error during OnUpdate: {ex.Message}");
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

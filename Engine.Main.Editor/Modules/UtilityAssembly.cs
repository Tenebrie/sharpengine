using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Abstract;
using SharpGen.Runtime;

namespace Engine.Main.Editor.Modules;

public class UtilityAssembly() : ModularAssembly("Engine.Module.Utility", EngineModule.Workspace)
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
        HostBackstage.Hypervisor = Editor.Hypervisor.Instance;
        HostBackstage.StartupInitialize();
        RegisterLaminaRenderers();
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
            Logger.Error($"[UtilityAssembly] Error during OnUpdate: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return base.Update(deltaTime);
        }
        return base.Update(deltaTime);
    }

    public override void Unload()
    {
        UnregisterLaminaRenderers();
        HostBackstage?.FreeImmediately();
        HostBackstage = null;
        base.Unload();
    }
}

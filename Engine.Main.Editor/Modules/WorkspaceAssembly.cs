using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Abstract;
using Engine.Main.Shared;

namespace Engine.Main.Editor.Modules;

public class WorkspaceAssembly(IEntryPoint entryPoint) : ModularAssembly("Engine.Module.Host", EngineModule.Workspace)
{
    internal IWorkspaceHost? HostBackstage { get; private set; }
    private IBaseEngineContract? Contract { get; set; }
    
    internal override IModularHost? GetHost() => HostBackstage;

    public override void Load()
    {
        base.Load();
        Contract = Loader.ProduceContract<IBaseEngineContract>();
        if (Contract == null)
        {
            Logger.Error("WorkspaceAssembly: Failed to load entry point settings.");
            return;
        }
        HostBackstage = (IWorkspaceHost)Activator.CreateInstance(Contract.MainBackstage)!;
        HostBackstage.Name = "host-" + Guid.NewGuid();
        HostBackstage.Hypervisor = entryPoint.Hypervisor;
        HostBackstage.StartupInitialize();
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
            Logger.Error($"[WorkspaceAssembly] Error during OnUpdate: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
        }
        base.Update(deltaTime);
    }

    public override void Unload()
    {
        HostBackstage?.FreeImmediately();
        HostBackstage = null;
        base.Unload();
    }
}

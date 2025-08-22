using Engine.Core.Contracts;
using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Abstract;

namespace Engine.Main.Editor.Modules;

public class WorkspaceAssembly() : ModularAssembly("Engine.Module.Host", EngineModule.Workspace)
{
    private double _updatesPausedFor = 0.0;
    internal IWorkspaceHost? HostBackstage { get; private set; }
    private IBaseEngineContract? Contract { get; set; }
    
    internal override IModularHost? GetHost() => HostBackstage;

    public override void Load()
    {
        base.Load();
        Contract = Loader.LoadAssembly<IBaseEngineContract>();
        if (Contract == null)
        {
            Logger.Error("WorkspaceAssembly: Failed to load entry point settings.");
            return;
        }
        HostBackstage = (IWorkspaceHost)Activator.CreateInstance(Contract.MainBackstage)!;
        HostBackstage.Name = "host-" + Guid.NewGuid();
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
        }
        catch (Exception ex)
        {
            Logger.Error($"Error during OnUpdate: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
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

using Engine.Core.Contracts;
using Engine.Core.EntitySystem;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;
using Engine.Main.Editor.Modules.Abstract;

namespace Engine.Main.Editor.Modules;

public class EditorHostAssembly(string assemblyName = "Engine.Module.Host") : GuestAssembly(assemblyName)
{
    private double _updatesPausedFor = 0.0;
    
    public override void Init()
    {
        base.Init();
        var loadedSettings = Host.Load<IBaseEngineContract>();
        if (loadedSettings == null)
        {
            Logger.Error("Failed to load EditorHost assembly settings.");
            return;
        }
        Settings = (IEngineContract<Backstage>)loadedSettings;
        Backstage = (Backstage)Activator.CreateInstance(Settings.MainBackstage)!;
        var backstageContract = (IHostBackstage)Backstage;
        backstageContract.RootSupervisor = new EditorHypervisor();
        if (Editor.UserGameAssembly.Backstage is not null)
            backstageContract.UserBackstage = Editor.UserGameAssembly.Backstage;
        Backstage.Name = "host-" + Guid.NewGuid();
        Backstage.GameplayContext = Editor.GameplayContext;
    }

    public override bool Update(double deltaTime)
    {
        if (_updatesPausedFor > 0.0)
        {
            _updatesPausedFor -= deltaTime;
            return base.Update(deltaTime);
        }

        if (Backstage == null)
            return base.Update(deltaTime);
        
        try
        {
            BackstageEventLoop.ProcessLogicFrame(Backstage, deltaTime);
        } catch (Exception ex)
        {
            Logger.Error($"Error during OnUpdate: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            _updatesPausedFor = 3.0;
            return false;
        }
        return base.Update(deltaTime);
    }
    
    public void NotifyAboutUserBackstage(Backstage? backstage)
    {
        if (Backstage is IHostBackstage hostContract)
            hostContract.UserBackstage = backstage;
    }
}

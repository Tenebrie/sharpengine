using Engine.Editor.HotReload.Modules.Abstract;
using Engine.User.Contracts;
using Engine.Worlds;
using Engine.Worlds.Entities;

namespace Engine.Editor.HotReload.Modules;

public class EditorHostAssembly(string assemblyName = "Engine.Editor.Host") : GuestAssembly(assemblyName)
{
    public override void Init()
    {
        base.Init();
        var loadedSettings = Host.Load<IBaseEngineContract>();
        if (loadedSettings == null)
        {
            Console.Error.WriteLine("Failed to load EditorHost assembly settings.");
            return;
        }
        Settings = (IEngineContract<Backstage>)loadedSettings;
        Backstage = (Backstage)Activator.CreateInstance(Settings.MainBackstage)!;
        Backstage.Name = "host-" + Guid.NewGuid();
    }

    public override bool Update(double deltaTime)
    {
        if (Backstage != null)
            BackstageEventLoop.ProcessLogicFrame(Backstage, deltaTime);
        return base.Update(deltaTime);
    }
}
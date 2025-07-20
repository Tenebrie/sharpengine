using Engine.Editor.HotReload.Abstract;
using Engine.User.Contracts;
using Engine.Worlds;
using Engine.Worlds.Entities;

namespace Engine.Editor.HotReload;

public class UserlandAssembly(string assemblyName = "User.Game") : GuestAssembly(assemblyName)
{
    public override void Init()
    {
        base.Init();
        var loadedSettings = Host.Load<IBaseEngineContract>();
        if (loadedSettings == null)
        {
            Console.Error.WriteLine("Failed to load UserGame assembly settings.");
            return;
        }
        Settings = (IEngineContract<Backstage>)loadedSettings;
        Backstage = (Backstage)Activator.CreateInstance(Settings.MainBackstage)!;
        Editor.EditorHostAssembly.NotifyAboutUserBackstage(Backstage);
        Backstage.Name = "guest-" + Guid.NewGuid();
        Backstage.GameplayContext = Editor.GameplayContext;
    }

    public override bool Update(double deltaTime)
    {
        if (Backstage != null)
            BackstageEventLoop.ProcessLogicFrame(Backstage, deltaTime);
        return base.Update(deltaTime);
    }

    protected override void Destroy()
    {
        Editor.EditorHostAssembly.NotifyAboutUserBackstage(null);
        base.Destroy();
    }
}
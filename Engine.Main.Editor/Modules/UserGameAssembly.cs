using Engine.Core.Contracts;
using Engine.Core.EntitySystem;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Abstract;

namespace Engine.Main.Editor.Modules;

public class UserGameAssembly() : GuestAssembly("User.Game", EngineModule.UserHost)
{
    private double _updatesPausedFor = 0.0;

    internal override bool IgnoresTimeScale => false;

    public override void Init()
    {
        base.Init();
        var loadedSettings = Host.LoadAssembly<IBaseEngineContract>();
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

    public override void Destroy()
    {
        Editor.EditorHostAssembly.NotifyAboutUserBackstage(null);
        base.Destroy();
    }
}
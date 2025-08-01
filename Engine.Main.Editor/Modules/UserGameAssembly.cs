﻿using Engine.Core.Contracts;
using Engine.Core.EntitySystem;
using Engine.Core.EntitySystem.Entities;
using Engine.Main.Editor.Modules.Abstract;

namespace Engine.Hypervisor.Editor.Modules;

public class UserGameAssembly(string assemblyName = "User.Game") : GuestAssembly(assemblyName)
{
    private double _updatesPausedFor = 0.0;
    
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
        Main.Editor.Editor.EditorHostAssembly.NotifyAboutUserBackstage(Backstage);
        Backstage.Name = "guest-" + Guid.NewGuid();
        Backstage.GameplayContext = Main.Editor.Editor.GameplayContext;
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
            Console.Error.WriteLine($"Error during Backstage update: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            _updatesPausedFor = 3.0;
            return false;
        }
        return base.Update(deltaTime);
    }

    protected override void Destroy()
    {
        Main.Editor.Editor.EditorHostAssembly.NotifyAboutUserBackstage(null);
        base.Destroy();
    }
}
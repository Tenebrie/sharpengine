using System.Diagnostics;
using Engine.Core.Enum;
using Engine.Core.Logging;
using Engine.Editor.HotReload.Abstract;
using Engine.User.Contracts;
using Engine.Worlds;
using Engine.Worlds.Entities;

namespace Engine.Editor.HotReload;

public class EditorHostAssembly(string assemblyName = "Engine.Editor.Host") : GuestAssembly(assemblyName)
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
        var backstageContract = (IEditorHostContract)Backstage;
        backstageContract.Editor = new EditorControls();
        if (Editor.RenderingAssembly.Renderer is not null)
            backstageContract.Renderer = Editor.RenderingAssembly.Renderer;
        if (Editor.UserlandAssembly.Backstage is not null)
            backstageContract.UserBackstage = Editor.UserlandAssembly.Backstage;
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
    
    public void NotifyAboutRenderer(IRendererContract? renderer)
    {
        if (Backstage is IEditorHostContract hostContract)
            hostContract.Renderer = renderer;
    }
    
    public void NotifyAboutUserBackstage(Backstage? backstage)
    {
        if (Backstage is IEditorHostContract hostContract)
            hostContract.UserBackstage = backstage;
    }
}

public class EditorControls : IEditorContract
{
    public void ReloadUserGame()
    {
        Editor.ReloadAssembly(Editor.UserlandAssembly);
    }

    public void SetGameplayContext(GameplayContext context)
    {
        Editor.GameplayContext = context;
        if (Editor.UserlandAssembly.Backstage is not null)
            Editor.UserlandAssembly.Backstage.GameplayContext = context;
        if (Editor.EditorHostAssembly.Backstage is not null)
            Editor.EditorHostAssembly.Backstage.GameplayContext = context;
        if (Editor.RenderingAssembly.Renderer is not null)
            Editor.RenderingAssembly.Renderer.SetGameplayContext(context);
    }

    public void SetGameplayTimeScale(double timeScale)
    {
        if (Editor.UserlandAssembly.Backstage is not null)
            Editor.UserlandAssembly.Backstage.TimeScale = timeScale;
    }
}

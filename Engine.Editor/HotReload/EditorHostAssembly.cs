using Engine.Core.Enum;
using Engine.Editor.HotReload.Abstract;
using Engine.User.Contracts;
using Engine.Worlds;
using Engine.Worlds.Entities;

namespace Engine.Editor.HotReload;

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
        if (Backstage != null)
            BackstageEventLoop.ProcessLogicFrame(Backstage, deltaTime);
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
}

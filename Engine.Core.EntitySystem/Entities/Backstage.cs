using Engine.Core.Assets;
using Engine.Core.Communication.Signals;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Modules;
using Engine.Core.EntitySystem.Services;
using Engine.Core.EntitySystem.Utilities;
using Engine.Core.Enum;
using Engine.Core.Modules;
using Silk.NET.Windowing;

namespace Engine.Core.EntitySystem.Entities;

public partial class Backstage : Scene
{
    public string Name { get; set; } = "Backstage";

    private GameplayContext _gameplayContext = GameplayContext.Editor;
    public GameplayContext GameplayContext
    {
        get => _gameplayContext;
        set
        {
            _gameplayContext = value;
            ProcessGameplayContextChanged();
        }
    }

    internal ServiceRegistry ServiceRegistry { get; } = new();

    public Backstage()
    {
        Backstage = this;
        ServiceRegistry.Backstage = this;
    }

    internal IWindow Window { get; set; } = null!;
    public IRootHypervisor RootHypervisor { get; set; } = null!;
    public IPhysicsModule? PhysicsModule => RootHypervisor.PhysicsModule;
    public IRenderingModule? RenderingModule => RootHypervisor.RenderingModule;
    public IWindow GetWindow() => Window;

    public T CreateScene<T>() where T : Scene, new()
    {
        return AdoptChild(new T());
    }

    public void NotifyModuleReloaded(EngineModule module) => ProcessModuleReload(module);

    [OnCreate]
    internal void OnCreate()
    {
        AdoptChild(ServiceRegistry);
        ServiceRegistry.Preload<CacheRevalidationService>();
        RunAssemblyStaticInit();
    }

    [OnUpdate]
    internal void OnUpdate(double deltaTime)
    {
        ServiceRegistry.Get<ReaperService>().Reap();
        ServiceRegistry.Get<InputService>().SendKeyboardHeldEvents(deltaTime);
    }

    private Camera? GetActiveCamera => FindActiveCamera(this);
    public Camera GetActiveCameraOrThrow()
    {
        var camera = GetActiveCamera;
        if (camera == null)
            throw new InvalidOperationException("No active camera found in the Backstage.");
        return camera;
    }

    private Camera? FindActiveCamera(Atom target)
    {
        if (target is Camera camera
            && ((camera.IsEditorCamera && GameplayContext == GameplayContext.Editor) || (!camera.IsEditorCamera && GameplayContext != GameplayContext.Editor)))
        {
            return camera;
        }

        foreach (var child in target.Children)
        {
            var foundCamera = FindActiveCamera(child);
            if (foundCamera != null)
                return foundCamera;
        }

        return null;
    }
}

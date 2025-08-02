using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Modules;
using Engine.Core.EntitySystem.Services;
using Engine.Core.EntitySystem.Utilities;
using Engine.Core.Enum;
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
    
    [OnInit]
    internal void OnInit()
    {
        AdoptChild(ServiceRegistry);
        ServiceRegistry.Preload<CacheRevalidationService>();
    }

    [OnUpdate]
    internal void OnUpdate(double deltaTime)
    {
        ServiceRegistry.Get<ReaperService>().Reap();
        ServiceRegistry.Get<InputService>().SendKeyboardHeldEvents(deltaTime);
    }
}

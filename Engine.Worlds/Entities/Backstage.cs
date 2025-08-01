using Engine.Core.Enum;
using Engine.Core.Logging;
using Engine.Worlds.Attributes;
using Engine.Worlds.Services;
using Engine.Worlds.Utilities;
using Silk.NET.Windowing;

namespace Engine.Worlds.Entities;

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

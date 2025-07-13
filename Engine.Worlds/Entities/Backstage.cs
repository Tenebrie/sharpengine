using Engine.Worlds.Attributes;
using Engine.Worlds.Services;
using Engine.Worlds.Utilities;
using Silk.NET.Windowing;

namespace Engine.Worlds.Entities;

public class Backstage : Atom
{
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

    [OnUpdate]
    internal void OnUpdate(double deltaTime)
    {
        ServiceRegistry.Get<ReaperService>().Reap();
        ServiceRegistry.Get<InputService>().SendKeyboardHeldEvents(deltaTime);
    }

    [OnDestroy]
    internal void OnDestroy()
    {
        Console.WriteLine("Destroying Backstage and its services.");
    }
}

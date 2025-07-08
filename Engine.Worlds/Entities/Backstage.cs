using Engine.Worlds.Attributes;
using Engine.Worlds.Services;
using Engine.Worlds.Utilities;
using Silk.NET.Windowing;

namespace Engine.Worlds.Entities;

public class Backstage : Atom
{
    // internal List<Actor> Actors { get; } = [];
    // internal List<Scene> Scenes { get; } = [];
    
    internal ServiceRegistry ServiceRegistry { get; } = new();
    
    public Backstage()
    {
        Backstage = this;
    }

    internal IWindow Window { get; set; } = null!;
    public IWindow GetWindow() => Window;
    
    public T CreateScene<T>() where T : Scene, new()
    {
        return RegisterChild(new T());
    }

    [OnUpdate]
    internal void OnUpdate()
    {
        ServiceRegistry.Get<ReaperService>().Reap();
    }

    [OnDestroy]
    internal void OnDestroy()
    {
        Console.WriteLine("Destroying Backstage and its services.");
    }
}

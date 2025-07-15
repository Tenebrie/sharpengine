using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Services;

namespace Engine.Worlds.Utilities;

public class ServiceRegistry : Atom
{
    private Dictionary<Type, Service> Services { get; } = new();
    
    internal T Get<T>() where T : Service, new()
    {
        var type = typeof(T);
        if (Services.TryGetValue(type, out var service))
        {
            return (T)service;
        }
        
        service = AdoptChild(new T());
        Services[type] = service;
        return (T)service;
    }
    
    internal void Preload<T>() where T : Service, new()
    {
        _ = Get<T>();
    }
}
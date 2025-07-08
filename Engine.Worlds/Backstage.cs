using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Worlds;

public class Backstage : Atom
{
    // public List<Scene> Scenes { get; } = [];
    // public List<Service> Services { get; } = [];
    
    public Backstage()
    {
        Backstage = this;
    }

    [OnUpdate]
    public void OnTick(double deltaTime)
    {
        Console.WriteLine("Sanity: " + Children.Count);
    }
    
    public T CreateScene<T>() where T : Scene, new()
    {
        return RegisterAtom(new T());
    }
}

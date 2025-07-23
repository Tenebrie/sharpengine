namespace Engine.Worlds.Entities;

public abstract class Actor : Spatial
{
    public T CreateActor<T>() where T : Actor, new()
    {
        // Find nearest Scene parent
        var scene = GetParent<Scene>();
        if (scene == null)
            throw new InvalidOperationException("Actor must be added to a Scene before creating components.");
        
        return scene.AdoptChild(new T());
    }
    
    protected T CreateComponent<T>() where T : ActorComponent, new()
    {
        return AdoptChild(new T());
    }
}
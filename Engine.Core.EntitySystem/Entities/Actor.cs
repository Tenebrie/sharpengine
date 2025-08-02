namespace Engine.Core.EntitySystem.Entities;

public abstract partial class Actor : Spatial
{
    public Scene ParentScene
    {
        get
        {
            // Find nearest Scene parent
            var scene = GetParent<Scene>();
            if (scene == null)
                throw new InvalidOperationException("Actor must be added to a Scene before accessing ParentScene.");
            return scene;
        }
    }
    public T CreateActor<T>() where T : Actor, new()
    {
        return ParentScene.AdoptChild(new T());
    }
    
    protected T CreateComponent<T>() where T : ActorComponent, new()
    {
        return AdoptChild(new T());
    }
}
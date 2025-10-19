namespace Engine.Core.EntitySystem.Entities;

public partial class Atom
{
    private void InitializeComponents()
    {
        var data = ReflectionDataCache.GetValueOrDefault(GetType());

        foreach (var reflection in data.ComponentFields)
        {
            if (reflection.GetValue(this) != null)
                continue;
            var component = reflection.Factory();
            reflection.SetValue(this, component);
            AdoptChild(component);
            if (this is Actor && component is ActorComponent actorComponent)
            {
                actorComponent.Actor = (Actor)this;
            }
        }
    }
    
    private void InitializeChildren()
    {
        foreach (var child in Children)
            child.Initialize();
    }
    
    public T? GetParent<T>() where T : Atom
    {
        var parent = Parent;
        while (parent != null)
        {
            if (parent is T t)
                return t;
            parent = parent.Parent;
        }
        return null;
    }
    
    public T? GetChild<T>() where T : Atom
    {
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var index = 0; index < Children.Count; index++)
        {
            var child = Children[index];
            if (child is T t)
                return t;
        }

        return null;
    }
    
    public List<T> GetChildren<T>() where T : Atom
    {
        var result = new List<T>();
        foreach (var child in Children)
        {
            if (child is T t)
                result.Add(t);
        }
        return result;
    }
    public List<T> GetChildren<T>(List<T> destination) where T : Atom
    {
        destination.Clear();
        foreach (var child in Children)
            if (child is T t) destination.Add(t);
        return destination;
    }
}
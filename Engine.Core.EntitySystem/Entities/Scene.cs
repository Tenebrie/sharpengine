namespace Engine.Core.EntitySystem.Entities;

public partial class Scene : Atom
{
    private List<Actor> _actors = [];
    public List<Actor> Actors
    {
        get
        {
            FindActors(ref _actors);
            return _actors;
        }
    }
    
    public void FindActors(ref List<Actor> actors)
    {
        actors.Clear();
        foreach (var child in Children)
            FindActors(child, ref actors);
    }
    
    private static void FindActors(Atom atom, ref List<Actor> actors)
    {
        if (atom is Actor actor)
            actors.Add(actor);
        
        foreach (var child in atom.Children)
            FindActors(child, ref actors);
    }

    public T CreateActor<T>() where T : Actor, new()
    {
        var actor = new T();
        AdoptChild(actor);
        return actor;
    }
}
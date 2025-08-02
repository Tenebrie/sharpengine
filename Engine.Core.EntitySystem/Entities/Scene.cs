namespace Engine.Core.EntitySystem.Entities;

public partial class Scene : Atom
{
    public List<Actor> Actors
    {
        get
        {
            List<Actor> actors = [];
            FindActors(this, ref actors);
            return actors;
        }
    }
    
    private void FindActors(Atom atom, ref List<Actor> actors)
    {
        if (atom is Actor actor)
            actors.Add(actor);
        
        foreach (var child in atom.Children)
            FindActors(child, ref actors);
    }

    public T CreateActor<T>() where T : Actor, new()
    {
        return AdoptChild(new T());
    }
}
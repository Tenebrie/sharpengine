namespace Engine.Worlds.Entities;

public class Scene : Atom
{
    // public List<Actor> Actors { get; } = [];

    public T CreateActor<T>() where T : Actor, new()
    {
        return RegisterChild(new T());
    }
}
using System.Collections;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Engine.Core.EntitySystem.Entities;

public partial class Atom
{
    public Atom? Parent { get; internal set; }
    
    private readonly List<Atom> _children = [];
    
    public readonly ChildrenList<Atom> Children;

    public Atom()
    {
        Children = new ChildrenList<Atom>(_children);
    }
    
    public T AdoptChild<T>(T atom) where T : Atom, new()
    {
        atom.Parent?.RemoveChild(atom);
        _children.Add(atom);
        atom.Parent = this;
        atom.Backstage = Backstage;
        if (_isInitialized && !atom._isInitialized)
            atom.Initialize();
        return atom;
    }
    
    public void RemoveChild(Atom atom)
    {
        if (atom.Parent != this)
            throw new InvalidOperationException("Atom is not a child of this parent.");
        _children.Remove(atom);
        atom.Parent = null;
    }
}

public sealed class ChildrenList<T>(List<T> items) : IReadOnlyList<T>
{
    public int Count => items.Count;
    public T this[int index] => items[index];

    public List<T>.Enumerator GetEnumerator() => items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}


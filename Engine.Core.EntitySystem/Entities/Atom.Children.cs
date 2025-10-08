using System.Buffers;
using System.Collections;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using Engine.Core.Logging;

namespace Engine.Core.EntitySystem.Entities;

public partial class Atom
{
    public Atom? Parent { get; internal set; }
    
    private readonly List<Atom> _children = [];
    
    public readonly ChildrenList<Atom> Children;

    public Atom()
    {
        Children = new ChildrenList<Atom>(_children, _lock);
    }
    
    private readonly ReaderWriterLockSlim _lock = new();
    public T AdoptChild<T>(T atom) where T : Atom, new()
    {
        _lock.EnterWriteLock();
        atom.Parent?.RemoveChild(atom);
        _children.Add(atom);
        atom.Parent = this;
        atom.Backstage = Backstage;
        if (_isInitialized && !atom._isInitialized)
            atom.Initialize();
        _lock.ExitWriteLock();
        return atom;
    }
    
    public void RemoveChild(Atom atom)
    {
        if (atom.Parent != this)
            throw new InvalidOperationException("Atom is not a child of this parent.");
        _lock.EnterWriteLock();
        _children.Remove(atom);
        _lock.ExitWriteLock();
        atom.Parent = null;
    }
}

public sealed class ChildrenList<T>(List<T> items, ReaderWriterLockSlim lockObject) : IReadOnlyList<T>
{
    public int Count => items.Count;
    public T this[int index] => items[index];

    public List<T>.Enumerator GetEnumerator() => items.GetEnumerator();

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    public PooledArray<T> Snapshot()
    {
        lockObject.EnterReadLock();
        try
        {
            int count = items.Count;
            var pool = ArrayPool<T>.Shared;
            T[] buffer = pool.Rent(count);
            items.CopyTo(buffer, 0);
            return new PooledArray<T>(buffer, count, pool);
        }
        finally
        {
            lockObject.ExitReadLock();
        }
    }
}

/// <summary>
/// A rented array wrapper that returns the buffer to the pool on Dispose.
/// The valid data is in [0, Length).
/// </summary>
public readonly struct PooledArray<T> : IDisposable
{
    private readonly ArrayPool<T>? _pool;

    public T[] Array { get; }
    public int Length { get; }

    internal PooledArray(T[] array, int length, ArrayPool<T> pool)
    {
        Array = array;
        Length = length;
        _pool = pool;
    }

    /// <summary>
    /// Returns a span over the valid portion of the buffer.
    /// </summary>
    public ReadOnlySpan<T> AsSpan() => new ReadOnlySpan<T>(Array, 0, Length);

    /// <summary>
    /// Return the buffer to the pool. Contents are not cleared.
    /// </summary>
    public void Dispose()
    {
        // If you need zeroing for sensitive data, change to clearArray: true
        _pool?.Return(Array, clearArray: false);
    }
}

using System.Collections.Concurrent;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;

namespace Engine.Module.Physics.Utilities;

public class AtomRegistrationHandler
{
    private long _idCounter = 0;
    private bool _cacheValid = false;
    private AtomHandle[] _cachedArray = [];
    // private readonly ConcurrentDictionary<long, AtomHandle> _registeredAtoms = new();
    private readonly Dictionary<long, AtomHandle> _registeredAtoms = new();

    public long Add(Spatial parent, PhysicsComponent component)
    {
        var rid = Interlocked.Increment(ref _idCounter);
        var atomHandle = new AtomHandle
        {
            Rid = rid,
            Parent = parent,
            Component = component,
            CollisionCandidates = [],
            WorldTransform = Transform.Identity,
        };
        _registeredAtoms.TryAdd(rid, atomHandle);
        _cacheValid = false;
        return rid;
    }

    public void Remove(long rid)
    {
        _cacheValid = false;
        _registeredAtoms.Remove(rid, out _);
    }

    private AtomList _cachedAtomList = new();
    public AtomList AsArray()
    {
        if (!_cacheValid)
        {
            _cachedAtomList.Load(_registeredAtoms.Values);
        }

        _cacheValid = true;
        return _cachedAtomList;
    }
}

public class AtomList
{
    private int _count = 0;
    private AtomHandle[] _array = [];
    
    public int Length => _count;
    public ref AtomHandle this[int index] => ref _array[index];
    
    public void Load(Dictionary<long, AtomHandle>.ValueCollection values)
    {
        _count = values.Count;
        if (_array.Length < values.Count)
            _array = new AtomHandle[values.Count * 2];
        values.CopyTo(_array, 0);
    }
    
    public Span<AtomHandle> AsSpan() => _array.AsSpan(0, _count);
}

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
    private readonly ConcurrentDictionary<long, AtomHandle> _registeredAtoms = new();

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
        _registeredAtoms.TryRemove(rid, out _);
    }

    public AtomHandle[] AsArray()
    {
        if (!_cacheValid)
            _cachedArray = _registeredAtoms.Values.ToArray();
        _cacheValid = true;
        return _cachedArray;
    }
}
using System.Collections.Concurrent;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;

namespace Engine.Module.Physics.Utilities;

public class AtomRegistrationHandler
{
    private long _idCounter = 0;
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
        return _idCounter;
    }

    public void Remove(long rid) => _registeredAtoms.TryRemove(rid, out _);

    private AtomHandle[] _scratchArray = [];
    public AtomHandle[] AsArray()
    {
        var atomCount = _registeredAtoms.Count;
        if (_scratchArray.Length < atomCount)
            Array.Resize(ref _scratchArray, atomCount);

        var count = 0;
        for (var i = 0; i < atomCount; i++)
        {
            var handle = _registeredAtoms.ElementAt(i);
            _scratchArray[count] = handle.Value;
            count++;
        }

        return _scratchArray;
    }
}
using System.Collections.Concurrent;
using Engine.Core.EntitySystem.Components.Physics;
using Engine.Core.EntitySystem.Entities;

namespace Engine.Module.Physics.Utilities;

public class AtomRegistrationHandler
{
    private long _idCounter = 0;
    private readonly ConcurrentDictionary<long, AtomHandle> _registeredAtoms = new();
    
    public long Add(Spatial parent, PhysicsComponent component)
    {
        var id = Interlocked.Increment(ref _idCounter);
        var atomHandle = new AtomHandle
        {
            Parent = parent,
            Component = component,
        };
        _registeredAtoms.TryAdd(id, atomHandle);
        return _idCounter;
    }

    public void Remove(long rid) => _registeredAtoms.TryRemove(rid, out _);

    private AtomHandle[] _scratchArray = [];
    public AtomHandle[] AsArray()
    {
        Array.Resize(ref _scratchArray, _registeredAtoms.Count);
    
        var count = 0;
        foreach (var handle in _registeredAtoms.Values)
        {
            _scratchArray[count] = handle;
            count++;
        }
    
        return _scratchArray;
    }
}
using Engine.Core.Common;
using Engine.Core.EntitySystem.Interfaces;

namespace Engine.Module.Rendering.Utilities;

public class RenderableRegistrationHandler
{
    private long _idCounter = 0;
    private bool _cacheValid = false;
    // private readonly ConcurrentDictionary<long, AtomHandle> _registeredAtoms = new();
    private readonly Lock _dictionaryLock = new();

    private static int ActiveBufferIndex => FrameCounter.Current % 2;
    private static int BackBufferIndex => 1 - FrameCounter.Current % 2;
    private readonly Dictionary<long, RenderableHandle> _registeredAtoms = new();
    private readonly Dictionary<long, RenderableHandle>[] _toAdd = new Dictionary<long, RenderableHandle>[2];
    private readonly Dictionary<long, RenderableHandle>[] _toUpdate = new Dictionary<long, RenderableHandle>[2];
    private readonly HashSet<long>[] _toRemove = new HashSet<long>[2];

    public RenderableRegistrationHandler()
    {
        _toAdd[0] = new Dictionary<long, RenderableHandle>();
        _toAdd[1] = new Dictionary<long, RenderableHandle>();
        _toUpdate[0] = new Dictionary<long, RenderableHandle>();
        _toUpdate[1] = new Dictionary<long, RenderableHandle>();
        _toRemove[0] = [];
        _toRemove[1] = [];
    }

    public long Add(IRenderable renderable)
    {
        var rid = Interlocked.Increment(ref _idCounter);
        var maybeRequest = renderable.ProduceRenderRequest();
        if (maybeRequest is not { } request)
            return rid;
        var atomHandle = new RenderableHandle
        {
            Rid = rid,
            Renderable = renderable,
            RenderRequest = new RenderRequest
            {
                Mesh = request.Mesh,
                Material = request.Material,
                RenderScript = request.RenderScript,
                
                InstanceCount = 0,
                MaterialInstances = [],
                InstanceTransforms = [],
                
                SortOrder = request.SortOrder,
            },
        };
        _toAdd[ActiveBufferIndex][rid] = atomHandle;

        return rid;
    }

    public void Update(long rid, IRenderable renderable)
    {
        var maybeRequest = renderable.ProduceRenderRequest();
        if (maybeRequest is not { } request)
            return;
        _toUpdate[ActiveBufferIndex][rid] = new RenderableHandle
        {
            Rid = rid,
            Renderable = renderable,
            RenderRequest = request,
        };
    }

    public void Remove(long rid)
    {
        _toAdd[ActiveBufferIndex].Remove(rid);
        _toUpdate[ActiveBufferIndex].Remove(rid);
        _toRemove[ActiveBufferIndex].Add(rid);
    }

    public void FlushPending()
    {
        lock (_dictionaryLock)
        {
            if (_toAdd[BackBufferIndex].Count == 0 && _toUpdate[BackBufferIndex].Count == 0 && _toRemove[BackBufferIndex].Count == 0)
                return;

            foreach (var kv in _toAdd[BackBufferIndex])
                _registeredAtoms[kv.Key] = kv.Value;
            foreach (var kv in _toUpdate[BackBufferIndex])
                _registeredAtoms[kv.Key] = kv.Value;
            foreach (var rid in _toRemove[BackBufferIndex])
                _registeredAtoms.Remove(rid);
        }

        _toAdd[BackBufferIndex].Clear();
        _toUpdate[BackBufferIndex].Clear();
        _toRemove[BackBufferIndex].Clear();

        _cacheValid = false;
    }

    private readonly AtomList _cachedAtomList = new();
    public AtomList AsArray()
    {
        lock (_dictionaryLock)
        {
            if (!_cacheValid)
                _cachedAtomList.Load(_registeredAtoms.Values);

            _cacheValid = true;
            return _cachedAtomList;
        }
    }
}

public class AtomList
{
    private int _count = 0;
    private RenderableHandle[] _array = [];
    
    public int Length => _count;
    public ref RenderableHandle this[int index] => ref _array[index];
    
    public void Load(Dictionary<long, RenderableHandle>.ValueCollection values)
    {
        _count = values.Count;
        if (_array.Length < values.Count)
            _array = new RenderableHandle[values.Count * 2];
        values.CopyTo(_array, 0);
    }
    
    public Span<RenderableHandle> AsSpan() => _array.AsSpan(0, _count);
}

public struct RenderableHandle
{
    public required long Rid;
    public required IRenderable Renderable;
    public required RenderRequest RenderRequest;
}

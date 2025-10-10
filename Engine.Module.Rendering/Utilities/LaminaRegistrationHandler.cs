using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Lamina;
using Engine.Core.Profiling;

namespace Engine.Module.Rendering.Utilities;

public class LaminaRegistrationHandler
{
    private long _idCounter = 0;
    private bool _cacheValid = false;
    private LaminaRenderableHandle[] _cachedArray = [];
    private readonly Lock _dictionaryLock = new();
    private readonly Dictionary<long, LaminaRenderableHandle> _registeredAtoms = new();
    private readonly Dictionary<long, LaminaRenderableHandle> _toAdd = new();
    private readonly Dictionary<long, LaminaRenderableHandle> _toUpdate = new();
    private readonly HashSet<long> _toRemove = new();

    public long Add(ILaminaRenderable renderable)
    {
        var rid = Interlocked.Increment(ref _idCounter);
        lock (_dictionaryLock)
        {
            var atomHandle = new LaminaRenderableHandle
            {
                Rid = rid,
                Renderable = renderable
            };
            _toAdd[rid] = atomHandle;
        }

        return rid;
    }

    public void Update(long rid, ILaminaRenderable renderable)
    {
        lock (_dictionaryLock)
        {
            _toUpdate[rid] = new LaminaRenderableHandle
            {
                Rid = rid,
                Renderable = renderable
            };
        }
    }

    public void Remove(long rid)
    {
        lock (_dictionaryLock)
            _toRemove.Add(rid);
    }

    public void FlushPending()
    {
        if (_toAdd.Count == 0 && _toUpdate.Count == 0 && _toRemove.Count == 0)
            return;
        lock (_dictionaryLock)
        {
            if (_toAdd.Count == 0 && _toUpdate.Count == 0 && _toRemove.Count == 0)
                return;

            foreach (var rid in _toRemove)
                _registeredAtoms.Remove(rid);
            foreach (var kv in _toUpdate)
                _registeredAtoms[kv.Key] = kv.Value;
            foreach (var kv in _toAdd)
                _registeredAtoms[kv.Key] = kv.Value;

            _toAdd.Clear();
            _toUpdate.Clear();
            _toRemove.Clear();

            _cacheValid = false;
        }
    }

    private readonly LaminaRenderableHandleList _cachedAtomList = new();
    public LaminaRenderableHandleList AsArray()
    {
        lock (_dictionaryLock)
        {
            if (!_cacheValid)
            {
                _cachedAtomList.Load(_registeredAtoms.Values);
            }

            _cacheValid = true;
            return _cachedAtomList;
        }
    }
}

public class LaminaRenderableHandleList
{
    private int _count = 0;
    private LaminaRenderableHandle[] _array = [];
    
    public int Length => _count;
    public ref LaminaRenderableHandle this[int index] => ref _array[index];
    
    public void Load(Dictionary<long, LaminaRenderableHandle>.ValueCollection values)
    {
        _count = values.Count;
        if (_array.Length < values.Count)
            _array = new LaminaRenderableHandle[values.Count * 2];
        values.CopyTo(_array, 0);
    }
    
    public Span<LaminaRenderableHandle> AsSpan() => _array.AsSpan(0, _count);
}

public struct LaminaRenderableHandle
{
    public required long Rid;
    public required ILaminaRenderable Renderable;
}

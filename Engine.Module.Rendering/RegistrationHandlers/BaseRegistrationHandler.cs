using Engine.Core.Common;
using Engine.Core.Modules.EntitySystem;

namespace Engine.Module.Rendering.RegistrationHandlers;

public abstract class BaseRegistrationHandler<TCreator, TData> where TCreator : IAtom where TData : struct
{
    private bool _cacheValid = false;
    protected readonly Lock DictionaryLock = new();

    protected static long ActiveBufferIndex => FrameCounter.Current % 2;
    protected static long BackBufferIndex => 1 - FrameCounter.Current % 2;
    protected readonly Dictionary<long, TData> Registered = new();
    protected readonly Dictionary<long, TData>[] ToUpdate = new Dictionary<long, TData>[2];
    private readonly HashSet<long>[] _toRemove = new HashSet<long>[2];

    protected BaseRegistrationHandler()
    {
        ToUpdate[0] = new Dictionary<long, TData>();
        ToUpdate[1] = new Dictionary<long, TData>();
        _toRemove[0] = [];
        _toRemove[1] = [];
    }

    public abstract void AddOrUpdate(long rid, TCreator creator);

    public void Remove(long rid)
    {
        ToUpdate[ActiveBufferIndex].Remove(rid);
        _toRemove[ActiveBufferIndex].Add(rid);
    }

    public void FlushPending()
    {
        lock (DictionaryLock)
        {
            if (ToUpdate[BackBufferIndex].Count == 0 && _toRemove[BackBufferIndex].Count == 0)
                return;

            foreach (var kv in ToUpdate[BackBufferIndex])
                Registered[kv.Key] = kv.Value;
            foreach (var rid in _toRemove[BackBufferIndex])
                Registered.Remove(rid);
        }

        ToUpdate[BackBufferIndex].Clear();
        _toRemove[BackBufferIndex].Clear();

        _cacheValid = false;
    }

    private readonly AtomList<TData> _cachedAtomList = new();
    public AtomList<TData> AsArray()
    {
        lock (DictionaryLock)
        {
            if (!_cacheValid)
                _cachedAtomList.Load(Registered.Values);

            _cacheValid = true;
            return _cachedAtomList;
        }
    }
}

public class AtomList<TData> where TData : struct
{
    private int _count = 0;
    private TData[] _array = [];
    
    public void Load(Dictionary<long, TData>.ValueCollection values)
    {
        _count = values.Count;
        if (_array.Length < values.Count)
            _array = new TData[values.Count * 2];
        values.CopyTo(_array, 0);
    }
    
    public Span<TData> AsSpan() => _array.AsSpan(0, _count);
}


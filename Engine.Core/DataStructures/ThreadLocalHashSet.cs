namespace Engine.Core.DataStructures;

public class ThreadLocalHashSet<TValue> : IDisposable
{
    private readonly ThreadLocal<List<TValue>> _localValues = new(() => new List<TValue>(1000), trackAllValues: true);

    public void Add(TValue item)
    {
        lock (_localValues)
        {
            _localValues.Value!.Add(item);
        }
    }

    public void Collect(ref HashSet<TValue> collection)
    {
        lock (_localValues)
        {
            foreach (var list in _localValues.Values)
            {
                foreach (var v in list)
                    collection.Add(v);

                list.Clear();
            }
        }
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _localValues.Dispose();
    }
}
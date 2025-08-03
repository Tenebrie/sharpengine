namespace Engine.Core.DataStructures;

public class ThreadLocalHashSet<TValue>
{
    private readonly ThreadLocal<List<TValue>> _localValues = new(() => new List<TValue>(1000), trackAllValues: true);

    public void Add(TValue item)
    {
        _localValues.Value!.Add(item);
    }

    public void Collect(ref HashSet<TValue> collection)
    {
        foreach (var list in _localValues.Values)
        {
            foreach (var v in list)
                collection.Add(v);

            list.Clear();
        }
    }
}
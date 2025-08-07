namespace Engine.Core.Assets;

public class AssetFinalizerManager
{
    private Action _finalizers = () => { };
    private readonly Dictionary<object, Action> _namedFinalizers = new();
    
    public void Register(Action finalizer)
    {
        _finalizers += finalizer;
    }
    public void Register(object key, Action finalizer)
    {
        _namedFinalizers.TryGetValue(key, out var existingFinalizer);
        if (existingFinalizer != null)
            return;
        _namedFinalizers[key] = finalizer;
    }

    public void Dispose()
    {
        _finalizers.Invoke();
        foreach (var finalizer in _namedFinalizers.Values)
        {
            finalizer.Invoke();
        }
    }
}

using System.Reflection;
using Engine.Core.Modules;

namespace Engine.Main.Game.Modules.Abstract;

public abstract class BundledAssembly(string assemblyName, EngineModule module)
{
    public string Name => assemblyName;
    internal EngineModule Module => module;
    internal abstract IModularHost GetHost();
    internal double TimeScale { get; set; } = 1;

    internal abstract void Load();
    internal abstract void Update(double deltaTime);
    internal abstract void Destroy();
    
    public TContract ProduceContract<TContract>() where TContract : class
    {
        var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == assemblyName);
        if (assembly == null)
            throw new InvalidOperationException($"Assembly '{assemblyName}' not found in the current AppDomain.");
        var type = assembly.GetTypes()
            .FirstOrDefault(ImplementsContract<TContract>);
        if (type == null)
            throw new InvalidOperationException($"No type implementing {typeof(TContract).FullName} was found in the current AppDomain.");
        return (TContract)Activator.CreateInstance(type)!;
    }
    
    private static bool ImplementsContract<TContract>(Type t)
    {
        return t.GetInterfaces().Any(i =>
        {
            if (!i.IsGenericType && i == typeof(TContract))
                return true;

            return i.IsGenericType &&
                   i.GetGenericTypeDefinition() == typeof(TContract);
        });
    }
}
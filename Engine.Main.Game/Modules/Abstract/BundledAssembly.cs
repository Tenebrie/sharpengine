using System.Reflection;
using Engine.Core.Modules;
using Engine.Main.Shared;

namespace Engine.Main.Game.Modules.Abstract;

public abstract class BundledAssembly(string assemblyName, EngineModule module) : IRootAssembly
{
    internal EngineModule Module => module;
    internal abstract IModularHost GetHost();
    internal double TimeScale { get; set; } = 1;

    internal Assembly Assembly = null!;

    internal virtual void Load()
    {
        Assembly = Assembly.Load(assemblyName);
    }
    public abstract void Update(double deltaTime);

    protected TContract ProduceContract<TContract>() where TContract : class
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
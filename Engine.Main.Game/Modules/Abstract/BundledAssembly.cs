using System.Reflection;
using Engine.Core.Logging;
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
    
    private Assembly GetTargetAssembly() => Assembly.Load("Engine.Core.Lamina");

    // String-only references; no compile-time ties.
    private const string WidgetAttributeSimpleName = "LaminaWidgetAttribute"; // applied as [LaminaWidget]
    private const string WidgetAttributeUsageName  = "LaminaWidget";         // safety if emitted without "Attribute"
    private const string WidgetBaseName            = "LaminaWidgetComponent`1";
    private const string RepositoryTypeName        = "LaminaRendererRepository";
    private const string RegisterMethodName        = "RegisterRenderer";      // RegisterRenderer<TLayout,TRenderer>()

    public void RegisterLaminaRenderers()
    {
        var asm = GetTargetAssembly();
        if (asm == null)
            throw new Exception("Target assembly not loaded.");

        var repoType = FindTypeByName(RepositoryTypeName);
        if (repoType == null)
            throw new Exception($"Could not find type '{RepositoryTypeName}' in loaded assemblies.");

        var registerOpen = repoType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == RegisterMethodName
                                 && m.IsGenericMethodDefinition
                                 && m.GetGenericArguments().Length == 2
                                 && m.GetParameters().Length == 0);
        if (registerOpen == null)
            throw new Exception($"Could not find method '{RegisterMethodName}' in loaded assemblies.");

        foreach (var rendererType in SafeGetTypes(asm))
        {
            if (rendererType == null ||
                rendererType.IsAbstract ||
                rendererType.IsGenericTypeDefinition) continue;

            // Only classes explicitly marked with [LaminaWidget]
            if (!HasLaminaWidgetAttribute(rendererType)) continue;

            // Infer TLayout from the generic base LaminaWidgetComponent<TLayout>
            var layoutType = TryGetLayoutTypeFromWidgetBase(rendererType);
            if (layoutType == null) continue;

            var register = registerOpen.MakeGenericMethod(layoutType, rendererType);
            register.Invoke(null, null);
        }
    }
    
    
    private static IEnumerable<Type> SafeGetTypes(Assembly asm)
    {
        try { return asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null)!; }
    }

    private static bool HasLaminaWidgetAttribute(MemberInfo type)
    {
        // No strong type; match by attribute type's simple name.
        // Use inherit:true so attributes on base classes are seen if applicable.
        var attrs = type.GetCustomAttributes(inherit: true);
        foreach (var a in attrs)
        {
            var n = a.GetType().Name;
            if (string.Equals(n, WidgetAttributeSimpleName, StringComparison.Ordinal) ||
                string.Equals(n, WidgetAttributeUsageName,   StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static Type? TryGetLayoutTypeFromWidgetBase(Type rendererType)
    {
        // Walk base types until we find LaminaWidgetComponent<TLayout>
        for (var t = rendererType; t != null; t = t.BaseType)
        {
            if (t.IsGenericType)
            {
                var def = t.GetGenericTypeDefinition();
                if (string.Equals(def.Name, WidgetBaseName, StringComparison.Ordinal))
                {
                    return t.GetGenericArguments()[0];
                }
            }
        }
        return null;
    }

    private static Type? FindTypeByName(string simpleName)
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in SafeGetTypes(asm))
            {
                if (t == null || !t.IsClass) continue;
                if (string.Equals(t.Name, simpleName, StringComparison.Ordinal))
                    return t;
            }
        }
        return null;
    }
}
using System.Reflection;
using Engine.Core.Communication.Tasks;
using Engine.Core.Modules;

namespace Engine.Main.Editor.Modules.Abstract;

public abstract class ModularAssembly(string assemblyName, EngineModule module) : LibraryAssembly(assemblyName)
{
    internal EngineModule Module => module;

    internal double TimeScale = 1.0;
    
    internal abstract IModularHost? GetHost();

    public override void Unload()
    {
        if (Loader.Assembly is not null)
        {
            MainThreadTask.Purge(Loader.Assembly);
            RenderThreadTask.Purge(Loader.Assembly);
        }
        base.Unload();
    }

    // Provided by you.
    private Assembly GetTargetAssembly() => Loader.Assembly!;

    // String-only references; no compile-time ties.
    private const string WidgetAttributeSimpleName = "LaminaWidgetAttribute"; // applied as [LaminaWidget]
    private const string WidgetAttributeUsageName  = "LaminaWidget";         // safety if emitted without "Attribute"
    private const string WidgetBaseName            = "LaminaWidgetComponent`1";
    private const string RepositoryTypeName        = "LaminaRendererRepository";
    private const string RegisterMethodName        = "RegisterRenderer";      // RegisterRenderer<TLayout,TRenderer>()
    private const string UnregisterMethodName      = "Unregister";            // Unregister<TLayout>()

    protected void RegisterLaminaRenderers()
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

    protected void UnregisterLaminaRenderers()
    {
        var asm = GetTargetAssembly();
        if (asm == null)
            throw new Exception("Target assembly not loaded.");

        var repoType = FindTypeByName(RepositoryTypeName);
        if (repoType == null)
            throw new Exception($"Could not find type '{RepositoryTypeName}' in loaded assemblies.");

        var unregisterOpen = repoType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m.Name == UnregisterMethodName
                                 && m.IsGenericMethodDefinition
                                 && m.GetGenericArguments().Length == 1
                                 && m.GetParameters().Length == 0);
        if (unregisterOpen == null)
            throw new Exception($"Could not find method '{UnregisterMethodName}' in loaded assemblies.");

        var layouts = new HashSet<Type>();

        foreach (var rendererType in SafeGetTypes(asm))
        {
            if (rendererType == null ||
                rendererType.IsAbstract ||
                rendererType.IsGenericTypeDefinition) continue;

            if (!HasLaminaWidgetAttribute(rendererType)) continue;

            var layoutType = TryGetLayoutTypeFromWidgetBase(rendererType);
            if (layoutType != null) layouts.Add(layoutType);
        }

        foreach (var layout in layouts)
        {
            var unregister = unregisterOpen.MakeGenericMethod(layout);
            unregister.Invoke(null, null);
        }
    }

    // ----------------- helpers -----------------

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
    
    public virtual void Destroy()
    {
        Unload();
    }
}
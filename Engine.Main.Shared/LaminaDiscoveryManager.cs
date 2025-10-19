using System.Reflection;

namespace Engine.Main.Shared;

public static class LaminaDiscoveryManager
{
    private const string WidgetAttributeName     = "LaminaWidget";
    private const string WidgetAttributeFullName = WidgetAttributeName + "Attribute";
    private const string WidgetBaseName          = "LaminaWidgetComponent`1";
    private const string RepositoryTypeName      = "LaminaRendererRepository";
    private const string RegisterMethodName      = "RegisterRenderer";

    public static void RegisterLaminaRenderers(Assembly sourceAssembly)
    {
        var repoType = FindLaminaRepositoryType();
        var registerOpen = repoType.GetMethods(BindingFlags.Public | BindingFlags.Static)
            .FirstOrDefault(m => m is { Name: RegisterMethodName, IsGenericMethodDefinition: true }
                                 && m.GetGenericArguments().Length == 2
                                 && m.GetParameters().Length == 0);
        if (registerOpen == null)
            throw new Exception($"Could not find method '{RegisterMethodName}' in '{RepositoryTypeName}'.");

        foreach (var rendererType in sourceAssembly.GetTypes())
        {
            if (rendererType.IsAbstract || rendererType.IsGenericTypeDefinition)
                continue;
            
            // Only classes explicitly marked with [LaminaWidget]
            if (!HasLaminaWidgetAttribute(rendererType))
                continue;

            // Infer TLayout from the generic base LaminaWidgetComponent<TLayout>
            var layoutType = TryGetLayoutTypeFromWidgetBase(rendererType);
            if (layoutType == null)
                continue;

            var register = registerOpen.MakeGenericMethod(layoutType, rendererType);
            register.Invoke(null, null);
        }
    }

    private static bool HasLaminaWidgetAttribute(MemberInfo type)
    {
        var attrs = type.GetCustomAttributes(inherit: true);
        return attrs.Select(a => a.GetType().Name)
            .Any(n => string.Equals(n, WidgetAttributeFullName, StringComparison.Ordinal) ||
                    string.Equals(n, WidgetAttributeName, StringComparison.Ordinal)
                );
    }

    private static Type? TryGetLayoutTypeFromWidgetBase(Type rendererType)
    {
        // Walk base types until we find LaminaWidgetComponent<TLayout>
        for (var t = rendererType; t != null; t = t.BaseType)
        {
            if (!t.IsGenericType)
                continue;
            
            var def = t.GetGenericTypeDefinition();
            if (string.Equals(def.Name, WidgetBaseName, StringComparison.Ordinal))
                return t.GetGenericArguments()[0];
        }
        return null;
    }

    private static Type FindLaminaRepositoryType()
    {
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var t in assembly.GetTypes())
            {
                if (!t.IsClass)
                    continue;
                if (string.Equals(t.Name, RepositoryTypeName, StringComparison.Ordinal))
                    return t;
            }
        }
        throw new Exception($"Could not find type '{RepositoryTypeName}' in loaded assemblies.");
    }
}
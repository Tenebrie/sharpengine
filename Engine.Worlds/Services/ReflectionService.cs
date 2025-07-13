using System.Reflection;
using Engine.Input.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Worlds.Services;

public abstract class ReflectionService : Service
{
    private static Dictionary<Type, Type?> LookupCache { get; } = new();
    public static Type? GetUserInputActionsEnum()
    {
        return GetUserTypeByAttributeCached<InputActionsAttribute>();
    }

    private static Type? GetUserTypeByAttributeCached<T>() where T : Attribute
    {
        if (LookupCache.TryGetValue(typeof(T), out var type))
            return type;
        var value = GetUserTypeByAttribute<T>();
        LookupCache.Add(typeof(T), value);
        return value;
    }
    
    private static Type? GetUserTypeByAttribute<T>() where T : Attribute
    {
        var inputActionEnum =
            AppDomain.CurrentDomain              // or Assembly.GetExecutingAssembly(), etc.
                .GetAssemblies()                 // walk every loaded assembly
                .SelectMany(a => a.GetTypes())   // flatten to all types
                .FirstOrDefault(t =>
                    t.IsEnum &&
                    t.GetCustomAttribute<T>() != null);

        return inputActionEnum;
    }
}
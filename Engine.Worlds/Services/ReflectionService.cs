using System.Reflection;
using Engine.Input.Attributes;
using Engine.Worlds.Entities;

namespace Engine.Worlds.Services;

public class ReflectionService : Service
{
    private Dictionary<Type, Type?> LookupCache { get; } = new();
    public Type? GetUserInputActionsEnum()
    {
        return GetUserTypeByAttributeCached<InputActionsAttribute>();
    }

    public void SetUserInputActionsEnum<TUserInputActions>() where TUserInputActions : Enum
    {
        LookupCache[typeof(InputActionsAttribute)] = typeof(TUserInputActions);
    }

    private Type? GetUserTypeByAttributeCached<T>() where T : Attribute
    {
        if (LookupCache.TryGetValue(typeof(T), out var type))
            return type;
        var value = GetUserTypeByAttribute<T>();
        LookupCache.Add(typeof(T), value);
        return value;
    }
    
    private Type? GetUserTypeByAttribute<T>() where T : Attribute
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
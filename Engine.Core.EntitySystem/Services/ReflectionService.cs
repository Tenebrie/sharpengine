using System.Reflection;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Input.Attributes;

namespace Engine.Core.EntitySystem.Services;

public partial class ReflectionService : Service
{
    private Dictionary<Type, Type?> LookupCache { get; } = new();
    public Type? GetUserInputActionsEnum()
    {
        return GetUserTypeByAttributeCached<InputActionsAttribute>();
    }

    public void SetUserInputActionsEnum<TUserInputActions>() where TUserInputActions : System.Enum
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
            AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .FirstOrDefault(t =>
                    t.IsEnum &&
                    t.GetCustomAttribute<T>() != null);

        return inputActionEnum;
    }
}
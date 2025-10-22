using System.Reflection;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.Input.Attributes;

namespace Engine.Core.EntitySystem.Services;

public partial class ReflectionService : Service
{
    private Dictionary<Type, HashSet<Type>> LookupCache { get; } = new();
    public HashSet<Type> GetUserInputActionsEnum()
    {
        return GetUserTypeByAttributeCached<InputActionsAttribute>();
    }

    public void SetUserInputActionsEnum<TUserInputActions>() where TUserInputActions : System.Enum
    {
        if (!LookupCache.TryGetValue(typeof(InputActionsAttribute), out var set))
            LookupCache[typeof(InputActionsAttribute)] = set = [];

        set.Add(typeof(TUserInputActions));
    }

    private HashSet<Type> GetUserTypeByAttributeCached<T>() where T : Attribute
    {
        if (LookupCache.TryGetValue(typeof(T), out var type))
            return type;
        var value = GetUserTypeByAttribute<T>();
        LookupCache.Add(typeof(T), value);
        return value;
    }

    private static HashSet<Type> GetUserTypeByAttribute<T>() where T : Attribute
    {
        var inputActionEnum =
            AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t =>
                    t.IsEnum &&
                    t.GetCustomAttribute<T>() != null)
                .ToHashSet();

        return inputActionEnum;
    }
}
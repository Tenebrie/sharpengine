namespace Engine.User.Codegen;

public static class LifecycleAttribute
{
    private static readonly string[] LifecycleAttributes =
    [
        "OnInit",
        "OnUpdate",
        "OnDestroy"
    ];
    
    private static string[] Get()
    {
        return LifecycleAttributes.Concat(LifecycleAttributes.Select(attr => attr + "Attribute")).ToArray();
    }
    
    public static bool Includes(string? name)
    {
        return name is not null && Get().Any(attr => attr == name);
    }
}
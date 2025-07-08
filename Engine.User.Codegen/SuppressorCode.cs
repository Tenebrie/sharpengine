namespace Engine.User.Codegen;

public enum SuppressorCode
{
    LifecycleMethodCantBeStatic,
}

public static class SuppressorCodeExtensions
{
    public static string GetCode(this SuppressorCode code)
    {
        return "Custom" + (code + 2000);
    }
}
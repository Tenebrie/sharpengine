namespace Engine.Roslyn;

public enum SuppressorCode
{
    LifecycleMethodCantBeStatic,
    ComponentDoesNotNeedToBeInitializedExplicitly,
}

public static class SuppressorCodeExtensions
{
    public static string GetCode(this SuppressorCode code)
    {
        return "TN" + (code + 2000);
    }
}

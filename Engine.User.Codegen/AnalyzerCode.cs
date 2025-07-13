namespace Engine.User.Codegen;

public enum AnalyzerCode
{
    MainBackstageAppliedIncorrectly,
    LifecycleAttributeOnPrivateMethod,
}

public static class ErrorCodeExtensions
{
    public static string GetCode(this AnalyzerCode code)
    {
        return "TN" + (code + 4000);
    }
}
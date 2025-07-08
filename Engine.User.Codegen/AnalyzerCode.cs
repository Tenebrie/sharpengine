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
        return "Custom" + (code + 4000);
    }
}
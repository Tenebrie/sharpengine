namespace Engine.User.Codegen;

public enum AnalyzerCode
{
    MainBackstageAppliedIncorrectly,
}

public static class ErrorCodeExtensions
{
    public static string GetCode(this AnalyzerCode code)
    {
        return "Custom" + (code + 4000);
    }
}
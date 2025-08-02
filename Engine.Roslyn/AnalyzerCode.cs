namespace Engine.Roslyn;

public enum AnalyzerCode
{
    MainBackstageAppliedIncorrectly,
    LifecycleAttributeOnPrivateMethod,
    AtomInheritedClassesMustBePartial,
    InputAttributeMatchesParametersAnalyzer,
}

public static class ErrorCodeExtensions
{
    public static string GetCode(this AnalyzerCode code)
    {
        return "TN" + (code + 4000);
    }
}
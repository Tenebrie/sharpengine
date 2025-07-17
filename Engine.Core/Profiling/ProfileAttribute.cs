using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using AspectInjector.Broker;
using JetBrains.Annotations;

namespace Engine.Core.Profiling;

[Injection(typeof(ProfileAspect))]
[AttributeUsage(AttributeTargets.Method)]
public class ProfileAttribute : Attribute;

[Aspect(Scope.Global)]
[SuppressMessage("Performance", "CA1822:Mark members as static")]
public class ProfileAspect
{
    [UsedImplicitly]
    [Advice(Kind.Before)] 
    public void OnEntry([Argument(Source.Name)] string name) => _sw = Profiler.Start();

    [UsedImplicitly]
    [Advice(Kind.After)]
    public void OnExit([Argument(Source.Name)] string name)
    {
        if (_sw == null)
            return;
        
        var method = MethodBase.GetCurrentMethod();
        if (method == null)
            throw new NullReferenceException("ProfileAttribute: How can MethodBase.GetCurrentMethod() return null?");
        
        var declaringType = method.DeclaringType;
        if (declaringType == null)
            throw new NullReferenceException("ProfileAttribute: Used on a method with no declaring type?");
        
        _sw.StopAndReportMethod(declaringType, method.Name);
    }

    [ThreadStatic] private static ProfilingStopwatch? _sw;
}

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
    [Advice(Kind.Around)]
    public object AroundInvoke(
        [Argument(Source.Metadata)] MethodBase method,
        [Argument(Source.Target)] Func<object[], object> target,
        [Argument(Source.Arguments)] object[] args)
    {
        // start timing
        var sw = Profiler.Start();

        try
        {
            return target(args);
        }
        finally
        {
            sw.StopAndReportMethod(method.DeclaringType!, method.Name);
        }
    }
}

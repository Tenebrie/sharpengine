using AspectInjector.Broker;
using Engine.Core.Profiling;
using JetBrains.Annotations;

namespace Engine.Worlds.Attributes;

[MeansImplicitUse]
[Injection(typeof(ProfileAspect))]
[AttributeUsage(AttributeTargets.Method)]
public sealed class OnUpdateAttribute : Attribute
{
}
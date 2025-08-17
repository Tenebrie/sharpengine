using AspectInjector.Broker;
using Engine.Core.Profiling.Attributes;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Attributes;

[MeansImplicitUse]
[Injection(typeof(ProfileAspect))]
[AttributeUsage(AttributeTargets.Method)]
public sealed class OnUpdateAttribute : Attribute
{
}
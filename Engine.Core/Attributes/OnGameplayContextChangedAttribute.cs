using JetBrains.Annotations;

namespace Engine.Core.Attributes;

[MeansImplicitUse]
// [Injection(typeof(ProfileAspect))]
[AttributeUsage(AttributeTargets.Method)]
public sealed class OnGameplayContextChangedAttribute : Attribute;

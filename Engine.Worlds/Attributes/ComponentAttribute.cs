using JetBrains.Annotations;

namespace Engine.Worlds.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ComponentAttribute : Attribute
{
}
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Attributes;

[MeansImplicitUse]
// TODO: Support properties
[AttributeUsage(AttributeTargets.Field)]
// [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class ComponentAttribute : Attribute
{
}
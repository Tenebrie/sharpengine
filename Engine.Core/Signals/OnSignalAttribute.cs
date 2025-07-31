using JetBrains.Annotations;

namespace Engine.Core.Signals;

[MeansImplicitUse]
// TODO: Support properties
[AttributeUsage(AttributeTargets.Method)]
// [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class OnSignalAttribute : Attribute
{
}
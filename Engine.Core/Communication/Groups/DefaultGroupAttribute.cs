using JetBrains.Annotations;

namespace Engine.Core.Communication.Groups;

[MeansImplicitUse]
// TODO: Support properties
[AttributeUsage(AttributeTargets.Field)]
// [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class DefaultGroupAttribute : Attribute;

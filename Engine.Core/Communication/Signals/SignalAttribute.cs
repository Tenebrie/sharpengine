using JetBrains.Annotations;

namespace Engine.Core.Communication.Signals;

[MeansImplicitUse]
// TODO: Support properties
[AttributeUsage(AttributeTargets.Field)]
// [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class SignalAttribute : Attribute;

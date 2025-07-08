using JetBrains.Annotations;

namespace Engine.Worlds.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public sealed class OnUpdate : Attribute
{
}
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Attributes;

/**
 * Informative attribute.
 * No runtime behavior, just used to mark engine classes.
 */
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public sealed class MainBackstageAttribute : Attribute;

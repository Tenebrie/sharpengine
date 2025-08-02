using JetBrains.Annotations;

namespace Engine.Core.Contracts.Attributes;

/**
 * Informative attribute.
 * No runtime behavior, just used to mark engine classes.
 */
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public class EngineSettingsAttribute : Attribute;

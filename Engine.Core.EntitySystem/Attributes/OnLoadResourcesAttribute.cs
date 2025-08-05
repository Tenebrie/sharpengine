using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class OnLoadResourcesAttribute : Attribute;
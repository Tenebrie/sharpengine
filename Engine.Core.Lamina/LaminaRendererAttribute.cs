using JetBrains.Annotations;

namespace Engine.Core.Lamina;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class LaminaRendererAttribute : Attribute;
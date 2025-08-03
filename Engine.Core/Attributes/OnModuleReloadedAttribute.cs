using Engine.Core.Modules;
using JetBrains.Annotations;

namespace Engine.Core.Attributes;

[MeansImplicitUse]
// [Injection(typeof(ProfileAspect))]
[AttributeUsage(AttributeTargets.Method)]
public sealed class OnModuleReloadAttribute(EngineModule module) : Attribute
{
    public EngineModule Module { get; } = module;
}

using Engine.Core.Modules.EntitySystem;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public class OnPrepareResourcesAttribute : Attribute, IOnPrepareResourcesAttribute;
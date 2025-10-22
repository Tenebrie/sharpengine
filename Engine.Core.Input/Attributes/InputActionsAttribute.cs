using JetBrains.Annotations;

namespace Engine.Core.Input.Attributes;

[PublicAPI]
[AttributeUsage(AttributeTargets.Enum)]
public class InputActionsAttribute : Attribute;
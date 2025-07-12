using JetBrains.Annotations;
using Silk.NET.Input;

namespace Engine.Input.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class OnInputActionAttribute(Key key, double value) : Attribute
{
    public Key Key = key;
    public double Value = value;
}

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class OnInputActionHeldAttribute(Key key, double value) : Attribute
{
    public Key Key = key;
    public double Value = value;
}

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class OnInputActionReleasedAttribute(Key key, double value) : Attribute
{
    public Key Key = key;
    public double Value = value;
}

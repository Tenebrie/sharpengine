using Silk.NET.Input;

namespace Engine.Core.Input.Attributes;

public enum InputParamBinding
{
    None,
    Double,
    Vector2,
    Vector3
}

public interface IOnInputBaseAttribute
{
    public long InputActionId { get; }
    public bool HasInputAction { get; }
    public Key? ExplicitKey { get; }
    public double X { get; }
    public double Y { get; }
    public double Z { get; }
    public InputParamBinding BindingParams { get; }
}

public interface IOnInputAttribute : IOnInputBaseAttribute;
public interface IOnInputHeldAttribute : IOnInputBaseAttribute;
public interface IOnInputReleasedAttribute : IOnInputBaseAttribute;
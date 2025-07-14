using Silk.NET.Input;

namespace Engine.Input.Attributes;

public enum InputParamBinding
{
    None,
    Double,
    Vector2
}

public interface IOnInputBaseAttribute
{
    public long InputActionId { get; }
    public bool HasInputAction { get; }
    public Key? ExplicitKey { get; }
    public double X { get; }
    public double Y { get; }
    public InputParamBinding BindingParams { get; }
}

public interface IOnInputAttribute : IOnInputBaseAttribute;
public interface IOnInputHeldAttribute : IOnInputBaseAttribute;
public interface IOnInputReleasedAttribute : IOnInputBaseAttribute;
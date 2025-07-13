using Silk.NET.Input;

namespace Engine.Input.Attributes;

public enum InputParamBinding
{
    None,
    Double,
    Vector2,
    Vector3,
}

public interface IOnInputAttribute
{
    public long InputActionId { get; }
    public bool HasInputAction { get; }
    public Key? ExplicitKey { get; }
    public double X { get; }
    public double Y { get; }
    public double Z { get; }
    public InputParamBinding BindingParams { get; }
}
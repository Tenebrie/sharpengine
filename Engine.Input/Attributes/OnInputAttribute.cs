using JetBrains.Annotations;
using Silk.NET.Input;

namespace Engine.Input.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public class OnInputAttribute<T>(T? action, Key? explicitKey, double x, double y, double z, InputParamBinding bindingParams)
    : Attribute, IOnInputAttribute where T : struct, Enum
{
    public OnInputAttribute(T action)
        : this(action, null, 0.0, 0.0, 0.0, InputParamBinding.None) {}
    public OnInputAttribute(T action, double value)
        : this(action, null, value, 0.0, 0.0, InputParamBinding.Double) {}
    public OnInputAttribute(T action, double x, double y)
        : this(action, null, x, y, 0.0, InputParamBinding.Vector2) { }
    public OnInputAttribute(T action, double x, double y, double z)
        : this(action, null, x, y, z, InputParamBinding.Vector3) { }

    public OnInputAttribute(Key explicitKey)
        : this(null, explicitKey, 0.0, 0.0, 0.0, InputParamBinding.None) {}
    public OnInputAttribute(Key explicitKey, double value)
        : this(null, explicitKey, value, 0.0, 0.0, InputParamBinding.Double) {}
    public OnInputAttribute(Key explicitKey, double x, double y)
        : this(null, explicitKey, x, y, 0.0, InputParamBinding.Vector2) { }
    public OnInputAttribute(Key explicitKey, double x, double y, double z)
        : this(null, explicitKey, x, y, z, InputParamBinding.Vector3) { }

    public long InputActionId => Convert.ToInt64(action);
    public bool HasInputAction => action.HasValue;
    public Key? ExplicitKey => explicitKey;
    public double X => x;
    public double Y => y;
    public double Z => z;
    public InputParamBinding BindingParams => bindingParams;
}
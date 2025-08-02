using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Silk.NET.Input;

namespace Engine.Core.Input.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[SuppressMessage("ReSharper", "IntroduceOptionalParameters.Global")]
public class OnInputAttribute<T>(T? action, double x, double y, double z, InputParamBinding bindingParams)
    : Attribute, IOnInputAttribute where T : struct, System.Enum
{
    public OnInputAttribute(T action)
        : this(action, 0.0, 0.0, 0.0, InputParamBinding.None) {}
    public OnInputAttribute(T action, double value)
        : this(action, value, 0.0, 0.0, InputParamBinding.Double) {}
    public OnInputAttribute(T action, double x, double y)
        : this(action, x, y, 0.0, InputParamBinding.Vector2) { }
    public OnInputAttribute(T action, double x, double y, double z)
        : this(action, x, y, z, InputParamBinding.Vector3) { }

    public long InputActionId => Convert.ToInt64(action);
    public bool HasInputAction => true;
    public Key? ExplicitKey => null;
    public double X => x;
    public double Y => y;
    public double Z => z;
    public InputParamBinding BindingParams => bindingParams;
}
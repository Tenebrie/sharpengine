using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Silk.NET.Input;

namespace Engine.Input.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[SuppressMessage("ReSharper", "IntroduceOptionalParameters.Global")]
public class OnInputHeldAttribute<T>(T action, double x, double y, InputParamBinding bindingParams)
    : Attribute, IOnInputHeldAttribute where T : struct, Enum
{
    public OnInputHeldAttribute(T action)
        : this(action, 0.0, 0.0, InputParamBinding.None) {}
    public OnInputHeldAttribute(T action, double value)
        : this(action, value, 0.0, InputParamBinding.Double) {}
    public OnInputHeldAttribute(T action, double x, double y)
        : this(action, x, y, InputParamBinding.Vector2) { }
    
    public long InputActionId => Convert.ToInt64(action);
    public bool HasInputAction => true;
    public Key? ExplicitKey => null;
    public double X => x;
    public double Y => y;
    public InputParamBinding BindingParams => bindingParams;
}

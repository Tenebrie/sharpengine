using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Silk.NET.Input;

namespace Engine.Input.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[SuppressMessage("ReSharper", "IntroduceOptionalParameters.Global")]
public class OnKeyInputReleasedAttribute(Key explicitKey, double x, double y, InputParamBinding bindingParams)
    : Attribute, IOnInputAttribute
{
    public OnKeyInputReleasedAttribute(Key explicitKey)
        : this(explicitKey, 0.0, 0.0, InputParamBinding.None) {}
    public OnKeyInputReleasedAttribute(Key explicitKey, double value)
        : this(explicitKey, value, 0.0, InputParamBinding.Double) {}
    public OnKeyInputReleasedAttribute(Key explicitKey, double x, double y)
        : this(explicitKey, x, y, InputParamBinding.Vector2) { }
    
    public long InputActionId => 0;
    public bool HasInputAction => false;
    public Key? ExplicitKey => explicitKey;
    public double X => x;
    public double Y => y;
    public InputParamBinding BindingParams => bindingParams;
}
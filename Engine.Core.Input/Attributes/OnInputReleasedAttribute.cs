using JetBrains.Annotations;
using Silk.NET.Input;

namespace Engine.Core.Input.Attributes;

[PublicAPI]
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public partial class OnBaseInputReleasedAttribute(long actionId, double x, double y, double z, InputParamBinding binding)
    : Attribute, IOnInputReleasedAttribute
{
    public long InputActionId { get; } = actionId;
    public bool HasInputAction => true;
    public Key? ExplicitKey => null;
    public double X { get; } = x;
    public double Y { get; } = y;
    public double Z { get; } = z;
    public InputParamBinding BindingParams { get; } = binding;
}
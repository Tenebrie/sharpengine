﻿using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using Silk.NET.Input;

namespace Engine.Core.Input.Attributes;

[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
[SuppressMessage("ReSharper", "IntroduceOptionalParameters.Global")]
public class OnKeyInputAttribute(Key explicitKey, double x, double y, double z, InputParamBinding bindingParams)
    : Attribute, IOnInputAttribute
{
    public OnKeyInputAttribute(Key explicitKey)
        : this(explicitKey, 0.0, 0.0, 0.0, InputParamBinding.None) {}
    public OnKeyInputAttribute(Key explicitKey, double value)
        : this(explicitKey, value, 0.0, 0.0, InputParamBinding.Double) {}
    public OnKeyInputAttribute(Key explicitKey, double x, double y)
        : this(explicitKey, x, y, 0.0, InputParamBinding.Vector2) { }
    public OnKeyInputAttribute(Key explicitKey, double x, double y, double z)
        : this(explicitKey, x, y, z, InputParamBinding.Vector3) { }

    public long InputActionId => 0;
    public bool HasInputAction => false;
    public Key? ExplicitKey => explicitKey;
    public double X => x;
    public double Y => y;
    public double Z => z;
    public InputParamBinding BindingParams => bindingParams;
}
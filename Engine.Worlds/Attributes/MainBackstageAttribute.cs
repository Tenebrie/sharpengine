﻿using JetBrains.Annotations;

namespace Engine.Worlds.Attributes;

/**
 * Informative attribute.
 * No runtime behavior, just used to mark engine classes.
 */
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Class)]
public sealed class MainBackstageAttribute : Attribute;

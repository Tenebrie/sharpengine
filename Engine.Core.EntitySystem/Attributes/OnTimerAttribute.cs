using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Attributes;

/// <summary>
/// This method will be called on a timer, either:
/// <list type="bullet">
///     <item>Every <see cref="Frames"/> frames, or</item>
///     <item>Every <see cref="Seconds"/> seconds</item>
/// </list>
/// </summary>
[MeansImplicitUse]
// [Injection(typeof(ProfileAspect))]
[AttributeUsage(AttributeTargets.Method)]
public sealed class OnTimerAttribute : Attribute
{
    /// <summary>
    /// This method will be called every N frames, with the number of ticks per second depending on the current frame rate. <br/>
    /// Can be useful for heavier calculations that may want to run every few frames.
    /// </summary>
    public int Frames { get; set; } = -1;
    /// <summary>
    /// This method will be called every N seconds, independent of the current frame rate. <br/>
    /// This uses a high-resolution timer which is guaranteed to hit on the next frame after the specified time has passed.
    /// </summary>
    public double Seconds { get; set; } = double.NaN;
}
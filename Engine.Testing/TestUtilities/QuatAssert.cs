using Xunit.Sdk;

namespace Engine.Testing.TestUtilities;


public static class QuatAssert
{
    /// <summary>
    /// Asserts that two quaternions are equal within a given tolerance.
    /// Throws an XunitException listing any mismatched components.
    /// </summary>
    /// <param name="expected">The quaternion you expected.</param>
    /// <param name="actual">The quaternion you actually got.</param>
    /// <param name="epsilon">Maximum allowed absolute difference per component.</param>
    public static void Equal(
        Quat expected,
        Quat actual,
        double epsilon = 1e-6)
    {
        var errors = new List<string>();

        if (Math.Abs(expected.X - actual.X) > epsilon)
            errors.Add($"X: expected {expected.X:F8}, got {actual.X:F8} (Δ={Math.Abs(expected.X - actual.X):F8})");
        if (Math.Abs(expected.Y - actual.Y) > epsilon)
            errors.Add($"Y: expected {expected.Y:F8}, got {actual.Y:F8} (Δ={Math.Abs(expected.Y - actual.Y):F8})");
        if (Math.Abs(expected.Z - actual.Z) > epsilon)
            errors.Add($"Z: expected {expected.Z:F8}, got {actual.Z:F8} (Δ={Math.Abs(expected.Z - actual.Z):F8})");
        if (Math.Abs(expected.W - actual.W) > epsilon)
            errors.Add($"W: expected {expected.W:F8}, got {actual.W:F8} (Δ={Math.Abs(expected.W - actual.W):F8})");

        if (errors.Count <= 0) return;
        var message = "Quaternions differ beyond tolerance:\n  " +
                      string.Join("\n  ", errors);
        throw new XunitException(message);
    }
}
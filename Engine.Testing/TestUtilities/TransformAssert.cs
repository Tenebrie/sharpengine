using Engine.Core.Common;
using Xunit.Sdk;

namespace Engine.Testing.TestUtilities;

public static class TransformAssert
{
    private static List<string> CompareTransforms(
        Transform expected,
        Transform actual,
        double epsilon = 1e-6
    )
    {
        var errors = new List<string>();

        for (var x = 0; x < 4; x++)
        {
            for (var y = 0; y < 4; y++)
            {
                if (Math.Abs(expected.ToMatrix()[x, y] - actual.ToMatrix()[x, y]) > epsilon)
                {
                    errors.Add($"Matrix[{x}, {y}]: expected {expected.ToMatrix()[x, y]:F8}, got {actual.ToMatrix()[x, y]:F8} (Δ={Math.Abs(expected.ToMatrix()[x, y] - actual.ToMatrix()[x, y]):F8})");
                }
            }
        }

        return errors;
    }
    
    public static void Equal(
        Transform expected,
        Transform actual,
        double epsilon = 1e-6
    )
    {
        var errors = CompareTransforms(expected, actual, epsilon);
        if (errors.Count == 0) return;
        var message = "Transforms differ beyond tolerance:\n  " +
                      string.Join("\n  ", errors);
        throw new XunitException(message);
    }
    
    public static void NotEqual(
        Transform expected,
        Transform actual,
        double epsilon = 1e-6
    )
    {
        var errors = CompareTransforms(expected, actual, epsilon);
        if (errors.Count > 0) return;
        var message = "Transforms are equal within tolerance:\n  " +
                      string.Join("\n  ", errors);
        throw new XunitException(message);
    }
    
}

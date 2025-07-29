using Engine.Core.Common;

namespace Engine.Testing.Core.Common;

public class Vector3Test
{
    [Fact]
    public void NormalizesVector3()
    {
        var vector = new Vector3(2, 2, 2).NormalizeInPlace();
        Assert.Equal(1, vector.Length);
        Assert.Equal(0.57735026918962584, vector.X);
        Assert.Equal(0.57735026918962584, vector.Y);
        Assert.Equal(0.57735026918962584, vector.Z);
    }
    [Fact]
    public void NormalizesVector4()
    {
        var vector = new Vector4(2, 2, 2, 2).NormalizeInPlace();
        Assert.Equal(1, vector.Length);
        Assert.Equal(0.5, vector.X);
        Assert.Equal(0.5, vector.Y);
        Assert.Equal(0.5, vector.Z);
        Assert.Equal(0.5, vector.W);
    }
    
    [Fact]
    public void ScalesVector3()
    {
        var vector = new Vector3(100, 50, 25);
        vector.SetLengthIfNotZero(10);
        Assert.Equal(new Vector3(8.728715609439694, 4.364357804719847, 2.1821789023599236), vector);
    }
    
    [Fact]
    public void ScalesVector4()
    {
        var vector = new Vector4(100, 50, 25, 10);
        vector.SetLengthIfNotZero(11.5);
        Assert.Equal(new Vector4(10, 5, 2.5, 1), vector);
    }
    
    [Fact]
    public void CalculatesDistanceBetweenVector4()
    {
        var vector = new Vector4(123, 73, 25, 10);
        var other = new Vector4(200, 100, -50, -20);
        var distance = vector.DistanceTo(other);
        Assert.Equal(114.81724609134291, distance);
    }
    
    [Fact]
    public void CalculatesDistanceSquaredBetweenVector4()
    {
        var vector = new Vector4(123, 73, 25, 10);
        var other = new Vector4(200, 100, -50, -20);
        var distanceSquared = vector.DistanceSquaredTo(other);
        Assert.Equal(13183, distanceSquared);
    }
}

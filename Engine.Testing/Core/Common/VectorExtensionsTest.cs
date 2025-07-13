using Engine.Core.Common;

namespace Engine.Testing.Core.Common;

public class VectorExtensionsTest()
{
    [Fact]
    public void NormalizesVector3()
    {
        var vector = new Vector(2, 2, 2).NormalizedCopy();
        Assert.Equal(1, vector.Length);
        Assert.Equal(0.57735026918962584, vector.X);
        Assert.Equal(0.57735026918962584, vector.Y);
        Assert.Equal(0.57735026918962584, vector.Z);
    }
    [Fact]
    public void NormalizesVector4()
    {
        var vector = new Vector4(2, 2, 2, 2).NormalizedCopy();
        Assert.Equal(1, vector.Length);
        Assert.Equal(0.5, vector.X);
        Assert.Equal(0.5, vector.Y);
        Assert.Equal(0.5, vector.Z);
        Assert.Equal(0.5, vector.W);
    }
    
    [Fact]
    public void ScalesVector3()
    {
        var vector = new Vector(100, 50, 25);
        vector.SetLengthIfNotZero(10);
        Assert.Equal(new Vector(8.728715609439694, 4.364357804719847, 2.1821789023599236), vector);
    }
    
    [Fact]
    public void ScalesVector4()
    {
        var vector = new Vector4(100, 50, 25, 10);
        vector.SetLengthIfNotZero(11.5);
        Assert.Equal(new Vector4(10, 5, 2.5, 1), vector);
    }
}

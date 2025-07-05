using Engine.Core.Common;
using Engine.Testing.TestUtilities;
using Xunit.Abstractions;

namespace Engine.Testing.Core.Common;

public class TransformTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void TranslatesCorrectly()
    {
        Assert.Equal(new Vector(1, 1, 1), Transform.Identity.Position);
        
        Assert.Equal(new Vector(3, 5, 8),   Transform.FromTranslation(3, 5, 8).Position);
        Assert.Equal(new Vector(3, 5, 8),   Transform.Identity.Translate(3, 5, 8).Position);
        Assert.Equal(new Vector(6, 10, 16), Transform.Identity.Translate(3, 5, 8).Translate(3, 5, 8).Position);
    }
    
    [Fact]
    public void RotatesCorrectly()
    {
        Assert.Equal(Quat.Identity, Transform.Identity.Rotation);
        
        QuatAssert.Equal(QuatUtils.FromRotation(45, 0, 0),   Transform.FromRotation(45, 0, 0).Rotation);
        QuatAssert.Equal(QuatUtils.FromRotation(45, 30, 15), Transform.FromRotation(45, 30, 15).Rotation);
        QuatAssert.Equal(QuatUtils.FromRotation(45, 0, 0),   Transform.Identity.Rotate(45, 0, 0).Rotation);
        QuatAssert.Equal(QuatUtils.FromRotation(90, 0, 0),   Transform.FromRotation(45, 0, 0).Rotate(45, 0, 0).Rotation);
        
        QuatAssert.Equal(QuatUtils.FromRotation(0, 90, 45),  Transform.FromRotation(45, 0, 0).Rotate(0, 90, 0).Rotation);
    }
    
    [Fact]
    public void ScalesCorrectly()
    {
        Assert.Equal(new Vector(1, 1, 1), Transform.Identity.Scale);
        
        Assert.Equal(new Vector(3, 5, 8),   Transform.FromScale(3, 5, 8).Scale);
        Assert.Equal(new Vector(3, 5, 8),   Transform.Identity.Rescale(3, 5, 8).Scale);
        Assert.Equal(new Vector(9, 25, 64), Transform.Identity.Rescale(3, 5, 8).Rescale(3, 5, 8).Scale);
    }
    
    [Fact]
    public void AppliesMultipleOperationsCorrectly()
    {
        var transform = Transform.FromRotation(45, 30, 15).Translate(10, 100, 10).Rescale(2, 2, 2);
        QuatAssert.Equal(QuatUtils.FromRotation(45, 30, 15), transform.Rotation);
        Assert.Equal(new Vector(10, 100, 10), transform.Position);
        Assert.Equal(new Vector(2, 2, 2), transform.Scale);
    }
    
    [Fact]
    public void AppliesParentTransformScaleDuringMultiplication()
    {
        var parentTransform = Transform.FromTranslation(10, 100, 10).Rescale(2, 2, 2);
        var childTransform = Transform.FromTranslation(10, 100, 10);
        var result = childTransform * parentTransform;
        Assert.Equal(new Vector(30, 300, 30), result.Position);
    }
    
    [Fact]
    public void IgnoresChildTransformScaleDuringMultiplication()
    {
        var parentTransform = Transform.FromTranslation(10, 100, 10);
        var childTransform = Transform.FromTranslation(10, 100, 10).Rescale(2, 2, 2);
        var result = childTransform * parentTransform;
        Assert.Equal(new Vector(20, 200, 20), result.Position);
    }
    
    [Fact]
    public void AppliesParentTransformBasisDuringMultiplication()
    {
        var parentTransform = Transform.FromRotation(0, 90, 0);
        var childTransform = Transform.FromTranslation(0, 0, -100);
        var result = childTransform * parentTransform;
        
        VectorAssert.Equal(new Vector(100, 0, 0), result.Position);
    }
}

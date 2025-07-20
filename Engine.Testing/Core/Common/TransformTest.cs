using Engine.Core.Common;
using Engine.Core.Makers;
using Engine.Testing.TestUtilities;

namespace Engine.Testing.Core.Common;

public class TransformTest
{
    [Fact]
    public void TranslatesCorrectly()
    {
        Assert.Equal(new Vector(0, 0, 0),   Transform.Identity.Position);
        
        Assert.Equal(new Vector(3, 5, 8),   Transform.FromTranslation(3, 5, 8).Position);
        Assert.Equal(new Vector(3, 5, 8),   Transform.Identity.TranslateGlobal(3, 5, 8).Position);
        Assert.Equal(new Vector(6, 10, 16), Transform.Identity.TranslateGlobal(3, 5, 8).TranslateGlobal(3, 5, 8).Position);
    }
    
    [Fact]
    public void RotatesCorrectly()
    {
        Assert.Equal(Quat.Identity, Transform.Identity.Rotation);
        
        QuatAssert.Equal(QuatMakers.FromRotation(45, 0, 0),   Transform.FromRotation(45, 0, 0).Rotation);
        QuatAssert.Equal(QuatMakers.FromRotation(45, 30, 15), Transform.FromRotation(45, 30, 15).Rotation);
        QuatAssert.Equal(QuatMakers.FromRotation(45, 0, 0),   Transform.Identity.Rotate(45, 0, 0).Rotation);
        QuatAssert.Equal(QuatMakers.FromRotation(90, 0, 0),   Transform.FromRotation(45, 0, 0).Rotate(45, 0, 0).Rotation);
        
        QuatAssert.Equal(QuatMakers.FromRotation(0, 90, 45),  Transform.FromRotation(45, 0, 0).Rotate(0, 90, 0).Rotation);
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
        var transform = Transform.FromRotation(45, 30, 15).TranslateGlobal(10, 100, 10).Rescale(2, 2, 2);
        QuatAssert.Equal(QuatMakers.FromRotation(45, 30, 15), transform.Rotation);
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

    [Fact]
    public void InversesMatrixCorrectly()
    {
        var transform = Transform.FromTranslation(10, 100, 10).Rotate(15, 30, 45).Rescale(2, 3, 4);
        var inverse = transform.Inverse();
        
        // Verify that applying the inverse results in the identity transform
        var result = transform * inverse;
        TransformAssert.Equal(Transform.Identity, result);
        TransformAssert.NotEqual(transform, result);
    }
    
    [Fact]
    public void InversesMatrixCorrectlyWithRef()
    {
        var transform = Transform.FromTranslation(10, 100, 10).Rotate(15, 30, 45).Rescale(2, 3, 4);
        var inverse = Transform.Identity;
        transform.Inverse(ref inverse);
        
        // Verify that applying the inverse results in the identity transform
        var result = transform * inverse;
        TransformAssert.Equal(Transform.Identity, result);
        TransformAssert.NotEqual(transform, result);
    }
    
    [Fact]
    public void InverseWithoutScaleAppliesCorrectly()
    {
        var transform = Transform.FromTranslation(10, 100, 10).Rotate(15, 30, 45);
        var inverse = Transform.Identity;
        transform.InverseWithoutScale(ref inverse);
        
        // Verify that applying the inverse results in the identity transform
        var result = transform * inverse;
        TransformAssert.Equal(Transform.Identity, result);
        TransformAssert.NotEqual(transform, result);
    }

    public class Basis
    {
        [Fact]
        public void GetsValidBasis()
        {
            var identityBasis = Transform.Identity.Basis;
            VectorAssert.Equal(new Vector(1, 0, 0), identityBasis.XAxis);
            VectorAssert.Equal(new Vector(0, 1, 0), identityBasis.YAxis);
            VectorAssert.Equal(new Vector(0, 0, 1), identityBasis.ZAxis);
            
            var pitchUpBasis = Transform.FromRotation(90, 0, 0).Basis;
            VectorAssert.Equal(new Vector(1, 0, 0), pitchUpBasis.XAxis);
            VectorAssert.Equal(new Vector(0, 0, -1), pitchUpBasis.YAxis);
            VectorAssert.Equal(new Vector(0, 1, 0), pitchUpBasis.ZAxis);
            
            var yawRightBasis = Transform.FromRotation(0, 90, 0).Basis;
            VectorAssert.Equal(new Vector(0, 0, 1), yawRightBasis.XAxis);
            VectorAssert.Equal(new Vector(0, 1, 0), yawRightBasis.YAxis);
            VectorAssert.Equal(new Vector(-1, 0, 0), yawRightBasis.ZAxis);
            
            var rollRightBasis = Transform.FromRotation(0, 0, 90).Basis;
            VectorAssert.Equal(new Vector(0, -1, 0), rollRightBasis.XAxis);
            VectorAssert.Equal(new Vector(1, 0, 0), rollRightBasis.YAxis);
            VectorAssert.Equal(new Vector(0, 0, 1), rollRightBasis.ZAxis);
        }
        
        [Fact]
        public void RotatesAroundSingleAxis()
        {
            var pitchTransform = Transform.Identity.RotateAroundGlobal(Axis.Pitch, 90);
            var yawTransform = Transform.Identity.RotateAroundGlobal(Axis.Yaw, 90);
            var rollTransform = Transform.Identity.RotateAroundGlobal(Axis.Roll, 90);
            QuatAssert.Equal(QuatMakers.FromRotation(90, 0, 0), pitchTransform.Rotation);
            QuatAssert.Equal(QuatMakers.FromRotation(0, 90, 0), yawTransform.Rotation);
            QuatAssert.Equal(QuatMakers.FromRotation(0, 0, 90), rollTransform.Rotation);
        }

        [Fact]
        public void RotatesAroundGlobalAxis()
        {
            var transform = Transform.Identity
                .RotateAroundGlobal(Axis.Pitch, 90)
                .RotateAroundGlobal(Axis.Yaw, 90)
                .RotateAroundGlobal(Axis.Roll, -90);
            QuatAssert.Equal(QuatMakers.FromRotation(0, 90, 0), transform.Rotation);
        }
        
        [Fact]
        public void RotatesAroundLocalAxis()
        {
            var transform = Transform.Identity
                .RotateAroundLocal(Axis.Pitch, 90)
                .RotateAroundLocal(Axis.Yaw, 90)
                .RotateAroundLocal(Axis.Roll, 90);
            QuatAssert.Equal(QuatMakers.FromRotation(0, 90, 0), transform.Rotation);
        }
    }
}

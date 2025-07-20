using Engine.Core.Common;
using Engine.Core.Makers;
using Xunit.Abstractions;

namespace Engine.Testing.Core.Common;

public class MatrixMakersTest(ITestOutputHelper testOutputHelper)
{
    [Fact]
    public void CreatesValidMatrixFromTranslation()
    {
        var matrix = MatrixMakers.FromTranslation(10, 20, 30);
        Assert.Equal(10, matrix.M41);
        Assert.Equal(20, matrix.M42);
        Assert.Equal(30, matrix.M43);
    }
    
    [Fact]
    public void CreatesValidMatrixFromIdentityQuaternion()
    {
        var quaternion = Quat.Identity;
        var matrix = MatrixMakers.FromQuaternion(quaternion);
        
        Assert.Equal(1, matrix.M11);
        Assert.Equal(0, matrix.M12);
        Assert.Equal(0, matrix.M13);
        Assert.Equal(0, matrix.M14);
        
        Assert.Equal(0, matrix.M21);
        Assert.Equal(1, matrix.M22);
        Assert.Equal(0, matrix.M23);
        Assert.Equal(0, matrix.M24);
        
        Assert.Equal(0, matrix.M31);
        Assert.Equal(0, matrix.M32);
        Assert.Equal(1, matrix.M33);
        Assert.Equal(0, matrix.M34);
        
        Assert.Equal(0, matrix.M41);
        Assert.Equal(0, matrix.M42);
        Assert.Equal(0, matrix.M43);
        Assert.Equal(1, matrix.M44);
    }
    
    [Fact]
    public void CreatesValidMatrixFromQuaternion()
    {
        var rotation = new Rotator(45, 45, 45).ToQuat();
        var matrix = MatrixMakers.FromQuaternion(rotation);
        testOutputHelper.WriteLine(matrix.ToString());
        
        Assert.Equal(0.8920486638100034, matrix.M11);
        Assert.Equal(-0.06664587581055026, matrix.M12);
        Assert.Equal(0.4469983318002789, matrix.M13);
        Assert.Equal(0, matrix.M14);
        
        Assert.Equal(0.4469983318002789, matrix.M21);
        Assert.Equal(0.27596319193541485, matrix.M22);
        Assert.Equal(-0.8509035245341183, matrix.M23);
        Assert.Equal(0, matrix.M24);
        
        Assert.Equal(-0.06664587581055026, matrix.M31);
        Assert.Equal(0.958854860724115, matrix.M32);
        Assert.Equal(0.27596319193541485, matrix.M33);
        Assert.Equal(0, matrix.M34);
        
        Assert.Equal(0, matrix.M41);
        Assert.Equal(0, matrix.M42);
        Assert.Equal(0, matrix.M43);
        Assert.Equal(1, matrix.M44);
    }
    
    [Fact]
    public void CreatesValidMatrixFromScale()
    {
        var matrix = MatrixMakers.FromScale(10, 20, 30);
        Assert.Equal(10, matrix.M11);
        Assert.Equal(20, matrix.M22);
        Assert.Equal(30, matrix.M33);
    }
}

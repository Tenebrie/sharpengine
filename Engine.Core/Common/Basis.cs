namespace Engine.Core.Common;

public class Basis
{
    public Vector XAxis { get; set; } = Vector.UnitX;
    public Vector YAxis { get; set; } = Vector.UnitY;
    public Vector ZAxis { get; set; } = Vector.UnitZ;
    
    public Vector Up => YAxis;
    public Vector Down => -YAxis;
    public Vector Left => -XAxis;
    public Vector Right => XAxis;
    public Vector Forward => -ZAxis;
    public Vector Backward => ZAxis;
    
    public Vector TransformVector(Vector localVector)
    {
        return new Vector(
            localVector.X * XAxis.X + localVector.Y * YAxis.X + localVector.Z * ZAxis.X,
            localVector.X * XAxis.Y + localVector.Y * YAxis.Y + localVector.Z * ZAxis.Y,
            localVector.X * XAxis.Z + localVector.Y * YAxis.Z + localVector.Z * ZAxis.Z
        );
    }
}

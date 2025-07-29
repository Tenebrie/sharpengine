namespace Engine.Core.Common;

public class Basis
{
    public Vector3 XAxis { get; set; } = Vector3.UnitX;
    public Vector3 YAxis { get; set; } = Vector3.UnitY;
    public Vector3 ZAxis { get; set; } = Vector3.UnitZ;
    
    public Vector3 Up => YAxis;
    public Vector3 Down => -YAxis;
    public Vector3 Left => -XAxis;
    public Vector3 Right => XAxis;
    public Vector3 Forward => -ZAxis;
    public Vector3 Backward => ZAxis;
    
    public Vector3 TransformVector(Vector3 localVector)
    {
        return new Vector3(
            localVector.X * XAxis.X + localVector.Y * YAxis.X + localVector.Z * ZAxis.X,
            localVector.X * XAxis.Y + localVector.Y * YAxis.Y + localVector.Z * ZAxis.Y,
            localVector.X * XAxis.Z + localVector.Y * YAxis.Z + localVector.Z * ZAxis.Z
        );
    }
}

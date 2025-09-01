using Engine.Core.Common;

namespace Engine.Core.Geometry.Shapes;

public class PlaneShape(Vector3 normal, double offset)
{
    /**
     * Normal vector defining the plane
     */
    public Vector3 Normal { get; } = normal;
    
    /**
     * Distance from the origin along the normal
     */
    public double Offset { get; } = offset;
    
    public static PlaneShape FromNormal(Vector3 normal, double offset = 0.0)
    {
        return new PlaneShape(normal.Normalized(), offset);
    }
}
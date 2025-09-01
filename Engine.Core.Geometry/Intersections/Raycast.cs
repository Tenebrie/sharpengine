using Engine.Core.Common;
using Engine.Core.Geometry.Shapes;
using Engine.Core.Modules.EntitySystem;

namespace Engine.Core.Geometry.Intersections;

public static class Raycast
{
    public static bool IntersectPlane(ICamera camera, PlaneShape plane, Vector2 mousePos, out Vector3 intersectionPoint)
    {
        var cameraPosition = camera.WorldTransform.Position;
        var cameraForward = camera.WorldTransform.Basis.Forward;
        var cameraRight = camera.WorldTransform.Basis.Right;
        var cameraUp = camera.WorldTransform.Basis.Up;
        
        var halfFov = double.DegreesToRadians(camera.FieldOfView / 2.0);
        
        // Convert mouse position to normalized device coordinates (-1 to 1)
        var ndcX = -(mousePos.X / camera.Width * 2.0 - 1.0);
        var ndcY = mousePos.Y / camera.Height * 2.0 - 1.0;
        
        // Calculate ray direction in view space
        var rayDirViewX = ndcX * Math.Tan(halfFov) * camera.AspectRatio;
        var rayDirViewY = ndcY * Math.Tan(halfFov);
        const double rayDirViewZ = -1.0;
        
        // Transform ray direction from view space to world space
        var rayDirection = cameraForward * -rayDirViewZ + 
                                  cameraRight * -rayDirViewX + 
                                  cameraUp * -rayDirViewY;

        // Calculate intersection with the plane
        var point = RayPlaneIntersection(cameraPosition, rayDirection.Normalized(), plane);
        if (point is null)
        {
            intersectionPoint = Vector3.Zero;
            return false;
        }
        intersectionPoint = point.Value;
        return true;
    }
    
    private static Vector3? RayPlaneIntersection(Vector3 rayOrigin, Vector3 rayDirection, PlaneShape plane)
    {
        // Calculate the denominator (ray direction dot plane normal)
        var denominator = rayDirection.DotProduct(plane.Normal);
        
        // If denominator is close to zero, ray is parallel to plane
        if (Math.Abs(denominator) < -1e8)
            return null;
        
        // Calculate the distance from ray origin to plane
        var planePoint = plane.Normal * plane.Offset;
        var distance = (planePoint - rayOrigin).DotProduct(plane.Normal) / denominator;
        
        // If distance is negative, intersection is behind the ray origin
        if (distance < 0)
            return null;
        
        // Calculate intersection point
        return rayOrigin + rayDirection * distance;
    }
}
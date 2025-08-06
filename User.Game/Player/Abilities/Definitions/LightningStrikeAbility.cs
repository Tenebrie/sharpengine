using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Services;
using Engine.Core.Common;
using Engine.Core.Logging;
using JetBrains.Annotations;

namespace User.Game.Player.Abilities.Definitions;

[UsedImplicitly]
public partial class LightningStrikeAbility : ActorComponent, IAbility
{
    public void OnCast()
    {
        var activeCamera = Backstage.GetActiveCameraOrThrow();
        var mousePos = GetService<InputService>().GetMousePosition();
        
        // Get the intersection point with the ground plane (Y=0)
        var intersectionPoint = GetMouseWorldPositionOnGround(activeCamera, mousePos);
        Logger.Info(intersectionPoint.ToString());
        
        // Now you can use intersectionPoint for your lightning strike
        // intersectionPoint will be the 3D world position where the mouse ray hits the ground
    }
    
    /// <summary>
    /// Converts mouse screen position to world position on the ground plane (Y=0)
    /// </summary>
    /// <param name="camera">The active camera</param>
    /// <param name="mousePos">Mouse position in screen coordinates</param>
    /// <returns>World position where the mouse ray intersects the ground plane, or null if no intersection</returns>
    private Vector3? GetMouseWorldPositionOnGround(Camera camera, Vector2 mousePos)
    {
        // Get camera properties in world space
        var cameraPosition = camera.WorldTransform.Position;
        var cameraForward = camera.WorldTransform.Basis.Forward;
        var cameraRight = camera.WorldTransform.Basis.Right;
        var cameraUp = camera.WorldTransform.Basis.Up;
        
        // Get window size for aspect ratio
        var windowSize = Backstage.GetWindow().Size;
        var aspectRatio = windowSize.X / (double)windowSize.Y;
        
        // Calculate field of view (assuming 60 degrees as seen in Camera.cs)
        const double fov = 60.0 * Math.PI / 180.0;
        var halfFov = fov / 2.0;
        
        // Convert mouse position to normalized device coordinates (-1 to 1)
        var ndcX = mousePos.X / windowSize.X * 2.0 - 1.0;
        var ndcY = -(mousePos.Y / windowSize.Y * 2.0 - 1.0); // Flip Y axis
        Logger.Info(ndcX + " " + ndcY);
        
        // Calculate ray direction in view space
        var rayDirViewX = ndcX * Math.Tan(halfFov) * aspectRatio;
        var rayDirViewY = ndcY * Math.Tan(halfFov);
        var rayDirViewZ = -1.0; // Forward in view space
        
        // Transform ray direction from view space to world space
        var rayDirection = cameraForward * rayDirViewZ + 
                          cameraRight * (-rayDirViewX) + 
                          cameraUp * rayDirViewY;
        rayDirection = rayDirection.NormalizedCopy();

        // Define the ground plane (normal pointing up, at Y=0)
        var planeNormal = Vector3.UnitY; // (0, 1, 0)
        var planePoint = Vector3.Zero;   // (0, 0, 0)
        
        // Calculate intersection with the plane
        return RayPlaneIntersection(cameraPosition, rayDirection, planePoint, planeNormal);
    }
    
    /// <summary>
    /// Calculates the intersection point of a ray with a plane
    /// </summary>
    /// <param name="rayOrigin">Origin of the ray</param>
    /// <param name="rayDirection">Direction of the ray (should be normalized)</param>
    /// <param name="planePoint">A point on the plane</param>
    /// <param name="planeNormal">Normal vector of the plane (should be normalized)</param>
    /// <returns>Intersection point or null if no intersection</returns>
    private static Vector3? RayPlaneIntersection(Vector3 rayOrigin, Vector3 rayDirection, Vector3 planePoint, Vector3 planeNormal)
    {
        // Calculate the denominator (ray direction dot plane normal)
        var denominator = rayDirection.DotProduct(planeNormal);
        
        // If denominator is close to zero, ray is parallel to plane
        if (Math.Abs(denominator) < double.Epsilon)
            return null;
        
        // Calculate the distance from ray origin to plane
        var distance = (planePoint - rayOrigin).DotProduct(planeNormal) / denominator;
        
        // If distance is negative, intersection is behind the ray origin
        if (distance > 0)
        {
            Logger.Info("Dist is " + distance);
            return null;
        }
        
        // Calculate intersection point
        return rayOrigin + rayDirection * distance;
    }
}
using Engine.Core.Common;

namespace Engine.Core.Modules.EntitySystem;

public interface ICamera : ISpatial
{
    public struct Plane { public Vector3 Normal; public double D; }
    
    public double FieldOfView { get; set; }
    public double AspectRatio { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
    public bool IsEditorCamera { get; }
    public Transform AsCameraView();
    public Plane[] UpdateFrustumPlanes();
}
namespace Engine.Core.Modules.EntitySystem;

public interface ICamera : ISpatial
{
    public double FieldOfView { get; set; }
    public double AspectRatio { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
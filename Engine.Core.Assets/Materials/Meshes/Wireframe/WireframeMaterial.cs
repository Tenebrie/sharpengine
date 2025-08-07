namespace Engine.Core.Assets.Materials.Meshes.Wireframe;

public class WireframeMaterial(string shaderPath) : Material(shaderPath)
{
    public static readonly Material Shared = CreateFromDisk("Meshes/Wireframe/Wireframe");
}

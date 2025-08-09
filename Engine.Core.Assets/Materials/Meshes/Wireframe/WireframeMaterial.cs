namespace Engine.Core.Assets.Materials.Meshes.Wireframe;

public abstract class WireframeMaterial(string shaderPath) : Material(shaderPath)
{
    public static readonly MaterialInstance Shared = CreateFromDisk("Meshes/Wireframe/Wireframe").Instantiate();
}

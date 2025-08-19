using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Components.Rendering;
using Engine.Core.EntitySystem.Entities;
using User.Game.Player;

namespace User.Game.Actors;

public partial class HonseTerrain : Actor
{
    [Component]
    public StaticMeshComponent Mesh;
    
    [OnReady]
    protected void OnReady() 
    {
        Mesh.StaticMesh = StaticMesh.CreateFromDisk("Meshes/terrain-plain.obj");
        Mesh.MaterialInstance = MaterialBuilder.BeginFromFilesystem("Shaders/cube")
            .SetTextureMode(TextureAddressMode.Wrap)
            .Instantiate()
            .SetUvScale(2.5)
            .LoadTexture(Texture.CreateFromDisk("Textures/honse-terrain-looped.png"));
    }

    [OnUpdate]
    protected void OnUpdate(double deltaTime)
    {
        var camera = ParentScene.Actors.OfType<Camera>().First();
        Transform.Position = new Vector3(camera.WorldTransform.Position.X - 100, -5000, camera.WorldTransform.Position.Z - 2000);
        Mesh.MaterialInstance.UvOffset = (new Vector2(camera.WorldTransform.Position.X, -camera.WorldTransform.Position.Z) / -Transform.Position.Y).Downgrade();
    }
}

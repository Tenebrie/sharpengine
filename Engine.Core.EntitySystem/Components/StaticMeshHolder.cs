using Engine.Core.Assets;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Loaders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Native.Bgfx;

namespace Engine.Core.EntitySystem.Components;

public partial class StaticMeshHolder : ActorComponent
{
    private static MaterialInstance _fallbackMaterial = null!;
    private StaticMesh? _mesh;
    public StaticMesh Mesh
    {
        get => _mesh ?? throw new InvalidOperationException("Mesh is not set.");
        set
        {
            _mesh?.OnMeshLoaded.Disconnect(OnMeshLoaded);
            _mesh = value;
            _mesh.OnMeshLoaded.Connect(this, OnMeshLoaded);
            if (_mesh.IsValid)
                OnMeshLoaded(_mesh.Vertices);
        }
    }

    private MaterialInstance? _material;
    public MaterialInstance Material
    {
        get => _material ?? _fallbackMaterial;
        set => _material = value;
    }
    [Component] public BoundingSphereComponent BoundingSphere;

    [OnPrepareResources]
    protected static Material OnPrepareResources()
    {
        Console.WriteLine("PREPAREING");
        return MaterialBuilder.Begin(typeof(StaticMeshHolder))
            .SetTintColor(System.Drawing.Color.White)
            .SetSamplingTexture(false)
            .Compile();
    }

    [OnLoadResources]
    protected static void OnLoadResources(Material material)
    {
        _fallbackMaterial = AssetManager.InstantiateMaterial(material);
    }

    private void OnMeshLoaded(AssetVertex[] vertices)
    {
        BoundingSphere.Generate(vertices);
    }
    
    public Bgfx.StateFlags RenderFlags { get; set; } = Bgfx.StateFlags.None;
}
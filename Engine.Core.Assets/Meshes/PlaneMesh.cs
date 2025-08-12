using System.Reflection;
using Engine.Core.Assets.Meshes.Builtins;

namespace Engine.Core.Assets.Meshes;

public class PlaneMesh : StaticMesh
{
    private static readonly PlaneMesh Instance = new();
    public static PlaneMesh Shared => Instance.Load(Assembly.GetCallingAssembly());
    
    private bool _isLoaded = false;
    private PlaneMesh Load(Assembly callingAssembly)
    {
        if (_isLoaded)
            return this;
        _isLoaded = true;

        var verts = TessellatedPlaneMesh.CreateVerts();
        var indices = TessellatedPlaneMesh.CreateIndices();
        LoadInternal(verts, indices, WindingOrder.Cw);
        AssetManager.AssemblyShared(callingAssembly).Meshes.Put("Generated/PlaneMesh", this);
        return this;
    }
}
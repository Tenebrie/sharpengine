using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;

namespace Engine.Core.Assets;

public class PipelineAssetManager : IDisposable
{
    private readonly Dictionary<Tuple<StaticMesh, Material>, IPipelineState> _cachedPipelines = new();
    
    public IPipelineState Produce(StaticMesh mesh, Material material)
    {
        var key = Tuple.Create(mesh, material);
        if (_cachedPipelines.TryGetValue(key, out var pipeline))
            return pipeline;

        pipeline = PipelineBuilder.Compose(mesh.Pipeline, material.Pipeline);
        _cachedPipelines[key] = pipeline;
        return pipeline;
    }

    public void InvalidateAll()
    {
        foreach (var pipeline in _cachedPipelines.Values)
            pipeline.Dispose();
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        InvalidateAll();
    }
}

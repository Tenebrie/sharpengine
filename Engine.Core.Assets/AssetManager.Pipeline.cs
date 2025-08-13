using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;

namespace Engine.Core.Assets;

public class PipelineAssetManager : IDisposable
{
    private readonly Dictionary<int, IPipelineState> _cachedPipelines = new();
    
    public IPipelineState Produce(MeshPipeline mesh, MaterialPipeline material)
    {
        var pipelineHash = mesh.HashCode ^ material.HashCode;
        if (_cachedPipelines.TryGetValue(pipelineHash, out var pipeline))
            return pipeline;

        pipeline = PipelineBuilder.ComposeWithoutCache(mesh, material);
        _cachedPipelines[pipelineHash] = pipeline;
        return pipeline;
    }

    public void InvalidateAll()
    {
        foreach (var pipeline in _cachedPipelines.Values)
            pipeline.Dispose();
        _cachedPipelines.Clear();
    }
    
    public void Dispose()
    {
        GC.SuppressFinalize(this);
        InvalidateAll();
    }
}

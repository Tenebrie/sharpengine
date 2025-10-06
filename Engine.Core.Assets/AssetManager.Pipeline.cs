using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Logging;

namespace Engine.Core.Assets;

public class PipelineAssetManager : IDisposable
{
    private readonly Dictionary<string, IPipelineState> _cachedPipelines = new();

    public IPipelineState Produce(MeshPipeline mesh, MaterialPipeline material)
    {
        var pipelineHash = mesh.HashCode + material.HashCode;
        if (_cachedPipelines.TryGetValue(pipelineHash, out var pipeline))
            return pipeline;

        Logger.Info("Creating new PSO");
        pipeline = PipelineBuilder.ComposeWithoutCache(mesh, material);
        _cachedPipelines[pipelineHash] = pipeline;
        return pipeline;
    }

    public void InvalidateAll()
    {
        AssetManager.Shared.Materials.InvalidateInstanceCaches();

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

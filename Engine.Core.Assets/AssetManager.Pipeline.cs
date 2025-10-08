using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Logging;

namespace Engine.Core.Assets;

public class PipelineAssetManager : IDisposable
{
    private readonly Dictionary<(string Mesh, string Material), IPipelineState> _cachedPipelines = new();

    public IPipelineState Produce(MeshPipeline mesh, MaterialPipeline material)
    {
        if (_cachedPipelines.TryGetValue((mesh.HashCode, material.HashCode), out var pipeline))
            return pipeline;

        pipeline = PipelineBuilder.ComposeWithoutCache(mesh, material);
        _cachedPipelines[(mesh.HashCode, material.HashCode)] = pipeline;
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

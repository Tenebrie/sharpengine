using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Logging;
using Engine.Core.Memory;
using Engine.Core.Profiling;
using Engine.Module.Rendering.Utilities;

namespace Engine.Module.Rendering.Renderers.Atoms;

public class SceneRenderer(RenderingHost host)
{
    private readonly Dictionary<int, MergedRenderRequest> _renderRequestPool = new();
    
    internal void RenderAtomTree(RenderableHandle[] atomsToRender, int atomsToRenderCount)
    {
        _renderRequestPool.Clear();

        var stopwatch = Profiler.Start();
        for (var i = 0; i < atomsToRenderCount; i++)
        {
            var renderable = atomsToRender[i];
            // if (renderable is Atom atom && !Atom.IsValid(atom))
                // continue;
            var requestOptional = renderable.RenderRequest;
            if (!requestOptional.HasValue)
                continue;
            var request = requestOptional.Value;
            if (!_renderRequestPool.TryGetValue(request.HashCode, out var mergedRequest))
            {
                _renderRequestPool[request.HashCode] = MergedRenderRequest.Create(request);
                continue;
            }
            
            mergedRequest.MaterialInstances = MemoryManager.MergeArrays(MemoryDomain.Rendering,
                mergedRequest.MaterialInstances,
                request.InstanceCount, request.MaterialInstances
            );
            mergedRequest.InstanceTransforms = MemoryManager.MergeArrays(MemoryDomain.Rendering,
                mergedRequest.InstanceTransforms,
                request.InstanceCount, request.InstanceTransforms
            );
            mergedRequest.InstanceCount += request.InstanceCount;
            _renderRequestPool[request.HashCode] = mergedRequest;
        }
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingCombineRequests);
        
        stopwatch = Profiler.Start();
        var mergedRequests = _renderRequestPool.Values.ToList();
        mergedRequests.Sort((a, b) => a.SortOrder - b.SortOrder);

        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingSortRequests);
        
        stopwatch = Profiler.Start();
        RenderStats.DrawCalls += _renderRequestPool.Values.Count;
        foreach (var req in mergedRequests)
        {
            req.RenderScript.Render(
                RenderContext.Current.ImmediateContext,
                req.InstanceCount,
                req.Mesh,
                (TransformSnapshot[])req.InstanceTransforms.Array,
                req.Material,
                (MaterialInstanceSnapshot[])req.MaterialInstances.Array);
        }
        stopwatch.StopAndReport(typeof(RenderingHost), ProfilingContext.RenderingSubmitAtoms);
        MemoryManager.FreeDomain(MemoryDomain.Rendering);
    }
    
    /**
     * TODO: Instead of dynamically collecting the requests every frame, consider keeping them around, and rely on registration/deregistration
     * when entities are added/removed or their materials/meshes change.
     */
    private struct MergedRenderRequest
    {
        public required StaticMesh Mesh;
        public required Material Material;
        public required IRenderScript RenderScript;
    
        public required int InstanceCount;
        public required MemoryManager.ArrayHandle InstanceTransforms;
        public required MemoryManager.ArrayHandle MaterialInstances;

        public required int SortOrder;
    
        public static MergedRenderRequest Create(RenderRequest request)
        { 
            var req = new MergedRenderRequest  
            {
                Mesh = request.Mesh, 
                Material = request.Material, 
                RenderScript = request.RenderScript,
                InstanceCount = request.InstanceCount,
                InstanceTransforms = 
                    MemoryManager.ProduceArray<TransformSnapshot>(MemoryDomain.Rendering, request.InstanceCount),
                MaterialInstances =
                    MemoryManager.ProduceArray<MaterialInstanceSnapshot>(MemoryDomain.Rendering, request.InstanceCount),
                SortOrder = request.SortOrder
            };

            req.MaterialInstances = MemoryManager.MergeArrays(MemoryDomain.Rendering, req.MaterialInstances,
                request.InstanceCount, request.MaterialInstances);
            req.InstanceTransforms = MemoryManager.MergeArrays(MemoryDomain.Rendering, req.InstanceTransforms,
                request.InstanceCount, request.InstanceTransforms);
            return req;
        }
    }
}
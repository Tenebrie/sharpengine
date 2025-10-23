using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;

namespace Engine.Core.Assets.Renderers;

/**
 * Full abstraction for rendering
 */
public interface IRenderScript
{
    public static RenderScript Default { get; } = new();
    public static LaminaRenderScript LaminaWidget { get; } = new();

    public void Render(
        IDeviceContext device,
        int instanceCount,
        StaticMesh mesh,
        TransformSnapshot[] worldTransforms,
        Material material,
        MaterialInstanceSnapshot[] materialInstances,
        object? userData = null
    );
}

/**
 * Base implementation of a RenderScript
 */
public abstract class BaseRenderScript<T> : IRenderScript where T : struct
{
    protected abstract List<InstanceBufferTicket> WriteInstanceData(int instanceCount,
        TransformSnapshot[] transforms,
        MaterialInstanceSnapshot[] instances,
        T userData);

    public void Render(
        IDeviceContext device,
        int instanceCount,
        StaticMesh mesh,
        TransformSnapshot[] worldTransforms,
        Material material,
        MaterialInstanceSnapshot[] materialInstances,
        object? userData = null
    )
    {
        var extra = userData is null ? default : (T)userData;
        Render(device, instanceCount, mesh, worldTransforms, material, materialInstances, extra);
    }

    public void Render(
        IDeviceContext device,
        int instanceCount,
        StaticMesh mesh,
        TransformSnapshot[] worldTransforms,
        Material material,
        MaterialInstanceSnapshot[] materialInstances,
        T userData)
    {
        if (instanceCount <= 0)
            return;

        var tickets = WriteInstanceData(instanceCount, worldTransforms, materialInstances, userData);
        
        var context = RenderContext.Current;
        var pso = AssetManager.Shared.Pipelines.Produce(mesh.Pipeline, material.Pipeline);
        device.SetPipelineState(pso);

        mesh.BindForRendering();
        
        var srb = material.BindMaterial(pso);
        
        foreach (var ticket in tickets)
        {
            srb.GetVariableByName(ShaderType.Vertex, ShaderVariable.InstanceData)?.Set(ticket.View, SetShaderResourceFlags.None);
            srb.GetVariableByName(ShaderType.Vertex, ShaderVariable.ObjectIndex)?.Set(context.ObjectIndexBuffer, SetShaderResourceFlags.None);
        
            var map = device.MapBuffer(context.ObjectIndexBuffer, MapType.Write, MapFlags.Discard);
            unsafe
            {
                var p = (uint*)map.ToPointer();
                p[0] = (uint)ticket.StartIndex;
            }
            device.UnmapBuffer(context.ObjectIndexBuffer, MapType.Write);
            device.CommitShaderResources(srb, ResourceStateTransitionMode.Transition);

            device.DrawIndexed(new DrawIndexedAttribs
            {
                NumIndices = (uint)mesh.Indices.Length,
                IndexType = StaticMesh.IndexType,
                NumInstances = (uint)ticket.Count
            });
        }
    }
}
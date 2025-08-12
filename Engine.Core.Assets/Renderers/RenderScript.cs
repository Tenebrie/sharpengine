using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;

namespace Engine.Core.Assets.Renderers;

public class RenderScript
{
    public static RenderContext Context { get; set; }
    
    public static RenderScript Default { get; } = new();
    
    public void Render(
        int instanceCount,
        StaticMesh mesh,
        Transform[] worldTransforms,
        Material material,
        MaterialInstance[] materialInstances)
    {
        if (instanceCount <= 0)
            return;
        
        var instances = new List<InstanceData>(instanceCount);
        for (var i = 0; i < instanceCount; i++)
        {
            instances.Add(new InstanceData
            {
                WorldTransform = worldTransforms[i],
                Tint = materialInstances[i].Tint
            });
        }
        
        var ticket = Context.InstanceBuffer.Write(instances);
        
        var pso = AssetManager.Shared.Pipelines.Produce(mesh, material);
        Context.DeviceContext.SetPipelineState(pso);
        mesh.BindForRendering();
        
        var srb = materialInstances[0].ProduceResourceBinding(pso);
        srb.GetVariableByName(ShaderType.Vertex, ShaderVariable.InstanceData)?.Set(ticket.View, SetShaderResourceFlags.None);
        
        var map = Context.DeviceContext.MapBuffer<uint>(Context.ObjectIndexBuffer, MapType.Write, MapFlags.Discard);
        map[0] = (uint)ticket.StartIndex;
        Context.DeviceContext.UnmapBuffer(Context.ObjectIndexBuffer, MapType.Write);

        srb.GetVariableByName(ShaderType.Vertex, ShaderVariable.ObjectIndex)?.Set(Context.ObjectIndexBuffer, SetShaderResourceFlags.None);

        Context.DeviceContext.CommitShaderResources(srb, ResourceStateTransitionMode.Transition);

        Context.DeviceContext.DrawIndexed(new DrawIndexedAttribs
        {
            NumIndices = (uint)mesh.Indices.Length,
            IndexType = mesh.IndexType,
            NumInstances = (uint)ticket.Count
        });
    }
}
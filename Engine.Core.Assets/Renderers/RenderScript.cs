using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;

namespace Engine.Core.Assets.Renderers;

public class RenderScript : IRenderScript
{
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

        var context = RenderContext.Current;
        var ticket = context.InstanceBuffer.Write(instances);
        
        var pso = AssetManager.Shared.Pipelines.Produce(mesh.Pipeline, material.Pipeline);
        context.DeviceContext.SetPipelineState(pso);
        mesh.BindForRendering();
        
        var srb = materialInstances[0].BindMaterial(pso);
        srb.GetVariableByName(ShaderType.Vertex, ShaderVariable.InstanceData)?.Set(ticket.View, SetShaderResourceFlags.None);
        
        var map = context.DeviceContext.MapBuffer<uint>(context.ObjectIndexBuffer, MapType.Write, MapFlags.Discard);
        map[0] = (uint)ticket.StartIndex;
        context.DeviceContext.UnmapBuffer(context.ObjectIndexBuffer, MapType.Write);

        srb.GetVariableByName(ShaderType.Vertex, ShaderVariable.ObjectIndex)?.Set(context.ObjectIndexBuffer, SetShaderResourceFlags.None);

        context.DeviceContext.CommitShaderResources(srb, ResourceStateTransitionMode.Transition);

        context.DeviceContext.DrawIndexed(new DrawIndexedAttribs
        {
            NumIndices = (uint)mesh.Indices.Length,
            IndexType = mesh.IndexType,
            NumInstances = (uint)ticket.Count
        });
    }
}
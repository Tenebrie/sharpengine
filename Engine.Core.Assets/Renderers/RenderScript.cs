using System.Buffers;
using Diligent;
using Engine.Core.Assets.Builders;
using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Microsoft.Extensions.ObjectPool;

namespace Engine.Core.Assets.Renderers;

public class RenderScript : IRenderScript
{
    private static InstanceData[] _instanceDataPool = [];

    public void Render(
        IDeviceContext device,
        int instanceCount,
        StaticMesh mesh,
        Transform[] worldTransforms,
        Material material,
        MaterialInstance[] materialInstances)
    {
        if (instanceCount <= 0)
            return;

        if (_instanceDataPool.Length < instanceCount)
            Array.Resize(ref _instanceDataPool, instanceCount);
        
        for (var i = 0; i < instanceCount; i++)
        {
            _instanceDataPool[i] = new InstanceData
            {
                WorldTransform = worldTransforms[i],
                Tint = materialInstances[i].Tint,
                UvOffset = materialInstances[i].UvOffset,
                UvScale = materialInstances[i].UvScale
            };
        }

        var context = RenderContext.Current;
        var ticket = context.InstanceBuffer.Write(instanceCount, _instanceDataPool);
        
        var pso = AssetManager.Shared.Pipelines.Produce(mesh.Pipeline, material.Pipeline);
        device.SetPipelineState(pso); 
        mesh.BindForRendering();
        
        var srb = materialInstances[0].BindMaterial(pso);
        srb.GetVariableByName(ShaderType.Vertex, ShaderVariable.InstanceData).Set(ticket.View, SetShaderResourceFlags.None);
        
        var map = device.MapBuffer<uint>(context.ObjectIndexBuffer, MapType.Write, MapFlags.Discard);
        map[0] = (uint)ticket.StartIndex;
        device.UnmapBuffer(context.ObjectIndexBuffer, MapType.Write);

        srb.GetVariableByName(ShaderType.Vertex, ShaderVariable.ObjectIndex).Set(context.ObjectIndexBuffer, SetShaderResourceFlags.None);

        device.CommitShaderResources(srb, ResourceStateTransitionMode.Transition);

        device.DrawIndexed(new DrawIndexedAttribs
        {
            NumIndices = (uint)mesh.Indices.Length,
            IndexType = StaticMesh.IndexType,
            NumInstances = (uint)ticket.Count
        });
    }
}
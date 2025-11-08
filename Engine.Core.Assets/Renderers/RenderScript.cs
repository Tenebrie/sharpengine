using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;

namespace Engine.Core.Assets.Renderers;

public class RenderScript : BaseRenderScript<IRenderScript.DummyData>
{
    private static InstanceData[] _instanceDataPool = [];
    
    protected override List<InstanceBufferTicket> WriteInstanceData(
        int instanceCount,
        TransformSnapshot[] transforms,
        MaterialInstanceSnapshot[] instances,
        IRenderScript.DummyData[] userData)
    {
        if (_instanceDataPool.Length < instanceCount)
            Array.Resize(ref _instanceDataPool, instanceCount * 2);
        
        for (var i = 0; i < instanceCount; i++)
        {
            _instanceDataPool[i] = new InstanceData
            {
                WorldTransform = transforms[i].Data.Downgrade(),
                Tint = instances[i].Tint,
                UvOffset = instances[i].UvOffset,
                UvScale = instances[i].UvScale
            };
        }

        return RenderContext.Current.InstanceBuffer.Write(instanceCount, _instanceDataPool);
    }
}
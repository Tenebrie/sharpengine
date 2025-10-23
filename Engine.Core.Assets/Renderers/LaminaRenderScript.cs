using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Rendering;
using Engine.Core.Common;
using Engine.Core.Logging;

namespace Engine.Core.Assets.Renderers;

public class LaminaRenderScript : BaseRenderScript<LaminaRenderScript.UserData>
{
    private static LaminaInstanceData[] _instanceDataPool = [];
    
    public struct UserData
    {
        public required Vector4 BorderRadius;
    }
    
    protected override List<InstanceBufferTicket> WriteInstanceData(
        int instanceCount,
        TransformSnapshot[] transforms,
        MaterialInstanceSnapshot[] instances,
        UserData userData)
    {
        if (_instanceDataPool.Length < instanceCount)
            Array.Resize(ref _instanceDataPool, instanceCount * 2);
        
        for (var i = 0; i < instanceCount; i++)
        {
            _instanceDataPool[i] = new LaminaInstanceData
            {
                WorldTransform = transforms[i].Data.Downgrade(),
                Tint = instances[i].Tint,
                UvOffset = instances[i].UvOffset,
                UvScale = instances[i].UvScale,
                BorderRadius = userData.BorderRadius.Downgrade(),
            };
        }

        return RenderContext.Current.LaminaInstanceBuffer.Write(instanceCount, _instanceDataPool);
    }
}
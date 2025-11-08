using Engine.Core.Assets.Materials;
using Engine.Core.Assets.Meshes;
using Engine.Core.Assets.Renderers;
using Engine.Core.Common;
using Engine.Core.DataStructures;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Interfaces;
using Engine.Core.Logging;
using JetBrains.Annotations;

namespace Engine.Core.EntitySystem.Components.Rendering;

[UsedImplicitly]
public partial class LaminaMeshComponent : StaticMeshComponent
{
    public LaminaRenderScript.UserData? ExtraShaderParams { get; set; } = null;
    private readonly FrameBufferedSingletonArray<LaminaRenderScript.UserData> _extraShaderParamsBuffer = new();
    
    public override RenderRequest? ProduceRenderRequest()
    {
        var maybeRequest = base.ProduceRenderRequest();
        if (!maybeRequest.HasValue)
            return null;
        var req = maybeRequest.Value;
        req.ExtraShaderParams = ExtraShaderParams.HasValue ? _extraShaderParamsBuffer.Produce(ExtraShaderParams.Value) : null;
        return req;
    }
}

using Engine.Core.EntitySystem.Interfaces;

namespace Engine.Module.Rendering.RegistrationHandlers;

public class LaminaRegistrationHandler : BaseRegistrationHandler<ILaminaRenderable, LaminaRenderableHandle>
{
    public override void AddOrUpdate(long rid, ILaminaRenderable renderable)
    {
        ToUpdate[ActiveBufferIndex][rid] = new LaminaRenderableHandle
        {
            Rid = rid,
            Renderable = renderable
        };
    }
}

public struct LaminaRenderableHandle
{
    public required long Rid;
    public required ILaminaRenderable Renderable;
}

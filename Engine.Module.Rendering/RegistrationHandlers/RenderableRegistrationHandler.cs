using Engine.Core.EntitySystem.Interfaces;

namespace Engine.Module.Rendering.RegistrationHandlers;

public class RenderableRegistrationHandler : BaseRegistrationHandler<IRenderable, RenderableHandle>
{
    public override void AddOrUpdate(long rid, IRenderable renderable)
    {
        var maybeRequest = renderable.ProduceRenderRequest();
        if (maybeRequest is not { } request)
        {
            Remove(rid);
            return;
        }

        ToUpdate[ActiveBufferIndex][rid] = new RenderableHandle
        {
            Rid = rid,
            RenderRequest = request,
        };
    }
}

public struct RenderableHandle
{
    public required long Rid;
    public required RenderRequest RenderRequest;
}

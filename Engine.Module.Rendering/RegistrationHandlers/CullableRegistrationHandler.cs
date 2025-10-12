using Engine.Core.EntitySystem.Interfaces;

namespace Engine.Module.Rendering.RegistrationHandlers;

public class CullableRegistrationHandler : BaseRegistrationHandler<ICullable, CullableHandle>
{
    public override void AddOrUpdate(long rid, ICullable renderable)
    {
        var maybeRequest = renderable.ProduceCullingRequest();
        if (maybeRequest is not { } request)
        {
            Remove(rid);
            return;
        }

        ToUpdate[ActiveBufferIndex][rid] = new CullableHandle
        {
            Rid = rid,
            CullingRequest = request
        };
    }
}

public struct CullableHandle
{
    public required long Rid;
    public required CullingRequest CullingRequest;
}

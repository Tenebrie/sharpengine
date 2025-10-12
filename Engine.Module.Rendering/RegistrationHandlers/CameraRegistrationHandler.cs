using Engine.Core.Common;
using Engine.Core.Modules.EntitySystem;

namespace Engine.Module.Rendering.RegistrationHandlers;

public class CameraRegistrationHandler : BaseRegistrationHandler<ICamera, CameraRenderableHandle>
{
    public override void AddOrUpdate(long rid, ICamera camera)
    {
        ToUpdate[ActiveBufferIndex][rid] = new CameraRenderableHandle
        {
            Rid = rid,
            IsEditorCamera = camera.IsEditorCamera,
            FrustumPlanes = camera.UpdateFrustumPlanes(),
            InverseWorldTransform = camera.AsCameraView().Snapshot()
        };
    }

    public CameraRenderableHandle? FindActive(bool isEditor)
    {
        lock (DictionaryLock)
        {
            foreach (var handle in Registered.Values)
            {
                if (handle.IsEditorCamera != isEditor)
                    continue;

                return handle;
            }

            return null;
        }
    }
}

public struct CameraRenderableHandle
{
    public required long Rid;
    public required bool IsEditorCamera;
    public required ICamera.Plane[] FrustumPlanes;
    public required TransformSnapshot InverseWorldTransform;
}

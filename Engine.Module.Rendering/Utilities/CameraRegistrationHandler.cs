using Engine.Core.Common;
using Engine.Core.Modules.EntitySystem;

namespace Engine.Module.Rendering.Utilities;

public class CameraRegistrationHandler
{
    private long _idCounter = 0;
    private bool _cacheValid = false;
    private readonly Lock _dictionaryLock = new();
    private readonly Dictionary<long, CameraRenderableHandle> _registeredAtoms = new();
    private readonly Dictionary<long, CameraRenderableHandle> _toAdd = new();
    private readonly Dictionary<long, CameraRenderableHandle> _toUpdate = new();
    private readonly HashSet<long> _toRemove = new();

    public long Add(ICamera camera)
    {
        var rid = Interlocked.Increment(ref _idCounter);
        lock (_dictionaryLock)
        {
            var atomHandle = new CameraRenderableHandle
            {
                Rid = rid,
                IsEditorCamera = camera.IsEditorCamera,
                FrustumPlanes = camera.UpdateFrustumPlanes(),
                InverseWorldTransform = camera.AsCameraView().Snapshot()
            };
            _toAdd[rid] = atomHandle;
        }

        return rid;
    }

    public void Update(long rid, ICamera camera)
    {
        lock (_dictionaryLock)
        {
            _toUpdate[rid] = new CameraRenderableHandle
            {
                Rid = rid,
                IsEditorCamera = camera.IsEditorCamera,
                FrustumPlanes = camera.UpdateFrustumPlanes(),
                InverseWorldTransform = camera.AsCameraView().Snapshot()
            };
        }
    }

    public void Remove(long rid)
    {
        lock (_dictionaryLock)
            _toRemove.Add(rid);
    }

    public void FlushPending()
    {
        if (_toAdd.Count == 0 && _toUpdate.Count == 0 && _toRemove.Count == 0)
            return;
        lock (_dictionaryLock)
        {
            if (_toAdd.Count == 0 && _toUpdate.Count == 0 && _toRemove.Count == 0)
                return;

            foreach (var rid in _toRemove)
                _registeredAtoms.Remove(rid);
            foreach (var kv in _toUpdate)
                _registeredAtoms[kv.Key] = kv.Value;
            foreach (var kv in _toAdd)
                _registeredAtoms[kv.Key] = kv.Value;

            _toAdd.Clear();
            _toUpdate.Clear();
            _toRemove.Clear();

            _cacheValid = false;
        }
    }

    public CameraRenderableHandle? FindActive(bool isEditor)
    {
        lock (_dictionaryLock)
        {
            foreach (var handle in _registeredAtoms.Values)
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

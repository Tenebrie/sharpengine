using Engine.Core.EntitySystem.Services;
using Engine.Core.Modules.EntitySystem;

namespace Engine.Module.Physics.Utilities;

public class CacheRevalidationServiceHandler
{
    private readonly List<ICacheRevalidationService> _revalidationServices = [];

    public void Add(ICacheRevalidationService service) => _revalidationServices.Add(service);
    public void Remove(ICacheRevalidationService service) => _revalidationServices.Remove(service);

    public void DisableAll()
    {
        foreach (var cacheRevalidationService in _revalidationServices)
        {
            cacheRevalidationService.Disabled = true;
        }
    }

    public void EnableAll()
    {
        foreach (var cacheRevalidationService in _revalidationServices)
        {
            cacheRevalidationService.Disabled = false;
        }
    }
}
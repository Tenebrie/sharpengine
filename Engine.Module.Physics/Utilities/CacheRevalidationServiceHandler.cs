using Engine.Core.EntitySystem.Services;

namespace Engine.Module.Physics.Utilities;

public class CacheRevalidationServiceHandler
{
    private readonly List<CacheRevalidationService> _revalidationServices = [];
    
    public void Add(CacheRevalidationService service) => _revalidationServices.Add(service);
    public void Remove(CacheRevalidationService service) => _revalidationServices.Remove(service);

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
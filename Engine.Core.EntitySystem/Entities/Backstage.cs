using System.Diagnostics.CodeAnalysis;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.EntitySystem.Services;
using Engine.Core.EntitySystem.Utilities;
using Engine.Core.Enum;
using Engine.Core.Modules;
using Silk.NET.Windowing;

namespace Engine.Core.EntitySystem.Entities;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")] // Public properties are exposed to the user-facing code
public partial class Backstage : Scene
{
    internal ServiceRegistry ServiceRegistry { get; } = new();

    public Backstage()
    {
        Backstage = this;
        ServiceRegistry.Backstage = this;
    }

    public T CreateScene<T>() where T : Scene, new()
    {
        return AdoptChild(new T());
    }

    public void NotifyModuleReloaded(EngineModule module) => ProcessModuleReload(module);
    public void NotifyGameplayContextChanged(GameplayContext context) => ProcessGameplayContextChanged(context);

    [OnCreate]
    internal void OnCreate()
    {
        AdoptChild(ServiceRegistry);
        ServiceRegistry.Preload<CacheRevalidationService>();
        RunAssemblyStaticInit();
    }

    [OnUpdate]
    internal void OnUpdate(double deltaTime)
    {
        ServiceRegistry.Get<ReaperService>().Reap();
        ServiceRegistry.Get<InputService>().SendKeyboardHeldEvents(deltaTime);
    }

    private Camera? GetActiveCamera => FindActiveCamera(this);
    public Camera GetActiveCameraOrThrow()
    {
        var camera = GetActiveCamera;
        if (camera == null)
            throw new InvalidOperationException("No active camera found in the Backstage.");
        return camera;
    }

    private Camera? FindActiveCamera(Atom target)
    {
        if (target is Camera camera
            && ((camera.IsEditorCamera && GameplayContext == GameplayContext.Editor) || (!camera.IsEditorCamera && GameplayContext != GameplayContext.Editor)))
        {
            return camera;
        }

        foreach (var child in target.Children)
        {
            var foundCamera = FindActiveCamera(child);
            if (foundCamera != null)
                return foundCamera;
        }

        return null;
    }
}

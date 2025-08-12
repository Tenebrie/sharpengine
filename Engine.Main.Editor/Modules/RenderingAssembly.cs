using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Modules;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Abstract;
using Silk.NET.Windowing;

namespace Engine.Main.Editor.Modules;

internal class RenderingAssembly(IWindow window) : GuestAssembly("Engine.Module.Rendering", EngineModule.Rendering)
{
    private bool _isInitialized = false;
    internal IRenderingModule? RenderingModule { get; set; }
    internal IRenderingModuleBootstrap? RenderingBootstrap { get; set; }
    internal RenderingResources Resources { get; set; }
    private readonly List<Backstage> _backstages = [];

    internal override bool IgnoresTimeScale => true;

    public override void Init()
    {
        base.Init();
        RenderingModule = Host.LoadAssembly<IRenderingModule>();
        RenderingBootstrap = Host.LoadContract<IRenderingModuleBootstrap>();
        if (RenderingModule == null || RenderingBootstrap == null)
        {
            Console.Error.WriteLine("Failed to instantiate renderer.");
            return;
        }
        if (_isInitialized)
        {
            RenderingModule.HotInitialize(Resources, window);
        }
        else
        {
            Resources = RenderingBootstrap.Initialize(window);
            RenderingModule.HotInitialize(Resources, window);
            _isInitialized = true;
        }
        RenderingModule.SetGameplayContext(Editor.GameplayContext);
        foreach (var backstage in _backstages)
            RenderingModule.Register(backstage);
    }

    internal void Register(Backstage backstage)
    {
        _backstages.Add(backstage);
        RenderingModule?.Register(backstage);
    }

    internal void Unregister(Backstage backstage)
    {
        _backstages.Remove(backstage);
        RenderingModule?.Unregister(backstage);
    }

    public override void Destroy()
    {
        RenderingModule?.HotShutdown();
        base.Destroy();
    }

    public void DestroyPermanently()
    {
        _backstages.Clear();
        base.Destroy();
        RenderingModule?.HotShutdown();
    }
}
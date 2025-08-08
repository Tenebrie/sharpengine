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
    private readonly List<Backstage> _backstages = [];

    internal override bool IgnoresTimeScale => true;

    public override void Init()
    {
        base.Init();
        RenderingModule = Host.Load<IRenderingModule>();
        if (RenderingModule == null)
        {
            Console.Error.WriteLine("Failed to instantiate renderer.");
            return;
        }
        if (_isInitialized)
        {
            RenderingModule.HotInitialize(window);
        }
        else
        {
            RenderingModule.Initialize(window);
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
        RenderingModule?.DisconnectCallbacks();
        base.Destroy();
    }

    public void DestroyPermanently()
    {
        _backstages.Clear();
        base.Destroy();
        RenderingModule?.Shutdown();
    }
}
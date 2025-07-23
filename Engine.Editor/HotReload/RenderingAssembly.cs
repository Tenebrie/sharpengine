using Engine.Editor.HotReload.Abstract;
using Engine.User.Contracts;
using Engine.Worlds.Entities;
using Silk.NET.Windowing;

namespace Engine.Editor.HotReload;

internal class RenderingAssembly(IWindow window, WindowOptions opts, string assemblyName = "Engine.Rendering") : GuestAssembly(assemblyName)
{
    private bool _isInitialized = false;
    internal IRendererContract? Renderer { get; set; }
    private readonly List<Backstage> _backstages = [];

    public override void Init()
    {
        base.Init();
        Renderer = Host.Load<IRendererContract>();
        if (Renderer == null)
        {
            Console.Error.WriteLine("Failed to instantiate renderer.");
            return;
        }
        if (_isInitialized)
        {
            Renderer.HotInitialize(window, opts);
        }
        else
        {
            Renderer.Initialize(window, opts);
            _isInitialized = true;
        }
        Renderer.SetGameplayContext(Editor.GameplayContext);
        foreach (var backstage in _backstages)
            Renderer.Register(backstage);
        Editor.EditorHostAssembly.NotifyAboutRenderer(Renderer);
    }
    
    internal void Register(Backstage backstage)
    {
        _backstages.Add(backstage);
        Renderer?.Register(backstage);
    }
    
    internal void Unregister(Backstage backstage)
    {
        _backstages.Remove(backstage);
        Renderer?.Unregister(backstage);
    }

    protected override void Destroy()
    {
        base.Destroy();
        Renderer?.DisconnectCallbacks();
        Editor.EditorHostAssembly.NotifyAboutRenderer(null);
    }
}
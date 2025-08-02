using Engine.Core.EntitySystem.Entities;
using Engine.Core.EntitySystem.Modules;
using Engine.Main.Editor.Modules.Abstract;
using Silk.NET.Windowing;

namespace Engine.Hypervisor.Editor.Modules;

internal class RenderingAssembly(IWindow window, string assemblyName = "Engine.Module.Rendering") : GuestAssembly(assemblyName)
{
    private bool _isInitialized = false;
    internal IRenderingModule? RenderingModule { get; set; }
    private readonly List<Backstage> _backstages = [];

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
        RenderingModule.SetGameplayContext(Main.Editor.Editor.GameplayContext);
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

    protected override void Destroy()
    {
        base.Destroy();
        RenderingModule?.DisconnectCallbacks();
    }
}
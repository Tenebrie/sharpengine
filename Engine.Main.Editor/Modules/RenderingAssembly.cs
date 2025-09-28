using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Abstract;

namespace Engine.Main.Editor.Modules;

internal class RenderingAssembly() : ModularAssembly("Engine.Module.Rendering", EngineModule.Rendering)
{
    private bool _isInitialized = false;
    internal IRenderingHost? RenderingHost { get; private set; }
    private IRenderingModuleBootstrap? RenderingBootstrap { get; set; }
    private RenderingResources Resources { get; set; }
    
    internal override IModularHost? GetHost() => RenderingHost;
    internal override int ImplicitReloadPriority => 1;

    public override void Load()
    {
        base.Load();
        RenderingHost = Loader.ProduceContract<IRenderingHost>();
        RenderingBootstrap = Loader.ProduceContract<IRenderingModuleBootstrap>();
        if (RenderingHost == null || RenderingBootstrap == null)
        {
            Logger.Error("RenderingAssembly: Failed to instantiate the host or bootstrapper.");
            return;
        }
        RenderingHost.Hypervisor = Editor.Hypervisor.Instance;
        RenderingBootstrap.Hypervisor = Editor.Hypervisor.Instance;
        if (_isInitialized)
        {
            RenderingHost.InitializeResources(Resources);
            RenderingHost.InitializeRenderers();
        }
        else
        {
            Resources = RenderingBootstrap.Initialize();
            RenderingHost.InitializeResources(Resources);
            RenderingHost.RenderEngineLoadingScreen();
            RenderingHost.InitializeRenderers();
            _isInitialized = true;
        }
    }

    public override void Unload()
    {
        RenderingHost?.HotShutdown();
        RenderingHost = null;
        base.Unload();
    }

    public override void Destroy()
    {
        RenderingHost?.HotShutdown();
        if (_isInitialized)
            RenderingBootstrap?.Shutdown();
        base.Destroy();
    }
}
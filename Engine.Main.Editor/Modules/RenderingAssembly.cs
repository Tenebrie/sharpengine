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

    public override void Load()
    {
        base.Load();
        RenderingHost = Loader.LoadAssembly<IRenderingHost>();
        RenderingBootstrap = Loader.LoadContract<IRenderingModuleBootstrap>();
        if (RenderingHost == null || RenderingBootstrap == null)
        {
            Logger.Error("RenderingAssembly: Failed to instantiate the host or bootstrapper.");
            return;
        }
        RenderingHost.Hypervisor = Editor.Hypervisor.Instance;
        RenderingBootstrap.Hypervisor = Editor.Hypervisor.Instance;
        if (_isInitialized)
        {
            RenderingHost.HotInitialize(Resources);
        }
        else
        {
            Resources = RenderingBootstrap.Initialize();
            RenderingHost.HotInitialize(Resources);
            _isInitialized = true;
        }
    }

    protected override void Unload()
    {
        RenderingHost?.HotShutdown();
        base.Unload();
    }

    public override void Destroy()
    {
        RenderingHost?.HotShutdown();
        RenderingBootstrap?.Shutdown();
        base.Destroy();
    }
}
using Engine.Core.Logging;
using Engine.Core.Modules;
using Engine.Main.Game.Modules.Abstract;
using Engine.Module.Rendering;

namespace Engine.Main.Game.Modules;

internal class ShippingRenderingAssembly() : BundledAssembly("Engine.Module.Rendering", EngineModule.Rendering)
{
    internal RenderingHost RenderingHost { get; private set; } = null!;
    private RenderingHostBootstrap RenderingBootstrap { get; set; } = null!;
    private RenderingResources Resources { get; set; }
    
    internal override IModularHost GetHost() => RenderingHost;

    internal override void Load()
    {
        RenderingHost = new RenderingHost
        {
            Hypervisor = Game.Hypervisor.Instance
        };
        RenderingBootstrap = new RenderingHostBootstrap
        {
            Hypervisor = Game.Hypervisor.Instance
        };
        
        Resources = RenderingBootstrap.Initialize();
        RenderingHost.InitializeResources(Resources);
        RenderingHost.RenderEngineLoadingScreen();
        RenderingHost.InitializeRenderers();
    }

    internal override void Update(double deltaTime) {}

    internal override void Destroy()
    {
        RenderingHost.HotShutdown();
        RenderingBootstrap.Shutdown();
    }
}
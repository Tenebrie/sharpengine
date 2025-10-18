using System.Diagnostics;
using System.Runtime.InteropServices;
using Engine.Core.Common;
using Engine.Core.Communication.Tasks;
using Engine.Core.Logging;
using Engine.Core.Memory;
using Engine.Core.Modules;
using Engine.Core.Profiling;
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

        StartRenderThread();
    }
    
    private Thread? _renderThread;
    private enum RenderThreadState { Stopped, Running }
    private volatile RenderThreadState _renderThreadState = RenderThreadState.Stopped;
    private void StartRenderThread()
    {
        if (_renderThread is { IsAlive: true } && _renderThreadState == RenderThreadState.Running)
            return;
        
        _renderThreadState = RenderThreadState.Running;
        _renderThread = new Thread(RenderThreadLoop)
        {
            Name = "RenderThread",
            IsBackground = true
        };
        _renderThread.Start();
    }
    
    private readonly Barrier _renderStartBarrier = new(2);
    private readonly Barrier _renderEndBarrier = new(2);
    private void RenderThreadLoop()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            double lastFrameTime = 0;

            while (_renderThreadState != RenderThreadState.Stopped)
            {
                FrameCounter.Increment();
                RenderStats.Reset();
                
                _renderStartBarrier.SignalAndWait();
                if (_renderThreadState == RenderThreadState.Stopped)
                    break;
                
                var currentTime = stopwatch.Elapsed.TotalMicroseconds;
                var deltaTime = (currentTime - lastFrameTime) / 1000000.0;
                lastFrameTime = currentTime;
                
                RenderingHost?.RenderSingleFrame(deltaTime);
                RenderThreadTask.ExecuteAllQueued();
                MemoryManager.FreeDomain(MemoryDomain.Rendering);
                
                _renderEndBarrier.SignalAndWait();
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"RenderingAssembly: Render thread crashed: {ex}");
            throw;
        }
    }

    public void StartFrameRender() => _renderStartBarrier.SignalAndWait();
    public void WaitUntilFrameEnd() => _renderEndBarrier.SignalAndWait();

    private void StopRenderThread()
    {
        _renderThreadState = RenderThreadState.Stopped;
        _renderEndBarrier.RemoveParticipant();
        _renderStartBarrier.RemoveParticipant();
        _renderThread?.Join();
        _renderThread = null;
    }

    public override void Unload()
    {
        RenderingHost?.HotShutdown();
        RenderingHost = null;
        base.Unload();
    }

    public override void Destroy()
    {
        StopRenderThread();
        RenderingHost?.HotShutdown();
        if (_isInitialized)
            RenderingBootstrap?.Shutdown();
        base.Destroy();
    }
}

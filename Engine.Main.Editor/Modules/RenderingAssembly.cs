using System.Diagnostics;
using Engine.Core.Common;
using Engine.Core.Communication.Tasks;
using Engine.Core.Logging;
using Engine.Core.Memory;
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

        StartRenderThread();
    }
    
    private Thread? _renderThread;
    private enum RenderThreadState { Stopped, Running, Paused }
    private volatile RenderThreadState _renderThreadState = RenderThreadState.Stopped;
    private void StartRenderThread()
    {
        if (_renderThreadState == RenderThreadState.Paused)
        {
            _renderThreadState = RenderThreadState.Running;
            return;
        }
        
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
    
    private SemaphoreSlim _renderSemaphore = new(1, 1);
    private Barrier _renderBarrier = new(2);
    private void RenderThreadLoop()
    {
        try
        {
            var stopwatch = Stopwatch.StartNew();
            double lastFrameTime = 0;
        
            while (_renderThreadState != RenderThreadState.Stopped)
            {
                if (_renderThreadState == RenderThreadState.Paused)
                {
                    Thread.Sleep(25);
                    continue;
                }
                _renderBarrier.SignalAndWait();
                FrameCounter.Increment();
                
                var currentTime = stopwatch.Elapsed.TotalMicroseconds;
                var deltaTime = (currentTime - lastFrameTime) / 1000000.0;
                lastFrameTime = currentTime;
                
                RenderingHost?.RenderSingleFrame(deltaTime);
                RenderThreadTask.ExecuteAllQueued();
                MemoryManager.FreeDomain(MemoryDomain.Rendering);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"RenderingAssembly: Render thread crashed: {ex}");
            throw;
        }
    }

    public void AwaitRenderThread()
    {
        _renderBarrier.SignalAndWait();
    }
    
    private void PauseRenderThread()
    {
        _renderThreadState = RenderThreadState.Paused;
    }
    private void StopRenderThread()
    {
        _renderThreadState = RenderThreadState.Stopped;
        _renderBarrier.RemoveParticipant();
        _renderThread?.Join();
        _renderThread = null;
    }

    public override void Unload()
    {
        PauseRenderThread();
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
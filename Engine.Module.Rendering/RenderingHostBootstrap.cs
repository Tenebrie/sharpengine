using System.Diagnostics;
using System.Runtime.InteropServices;
using Diligent;
using Engine.Core.Assets;
using Engine.Core.Logging;
using Engine.Core.Modules;
using JetBrains.Annotations;
using Silk.NET.Windowing;
using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using Vortice.DXGI;

namespace Engine.Module.Rendering;

/**
 * Excluded from hot reload - restart the application to apply changes in this class.
 */
[UsedImplicitly]
public class RenderingHostBootstrap : IRenderingModuleBootstrap
{
    public required IRootHypervisor Hypervisor { get; set; }
    private IRenderDevice _renderDevice = null!;
    private IDeviceContext _immediateContext = null!;
    private IDeviceContext[] _deferredContexts = [];
    private ISwapChain _swapChain = null!;
    private IEngineFactoryD3D12 _engineFactory = null!;
    private static IEngineFactory.MessageCallbackDelegate _messageCallback = null!;
    [UsedImplicitly] private static GCHandle _sCallbackRoot;
    
    public RenderingResources Initialize()
    {
        _engineFactory = Native.GetEngineFactoryD3D12();
        SetMessageCallback(_engineFactory);
        CreateRenderDeviceAndSwapChain(
            _engineFactory,
            out var renderDeviceOut,
            out _immediateContext,
            out _deferredContexts,
            out var swapChainOut,
            Hypervisor.Window
        );
        _renderDevice = renderDeviceOut;
        _swapChain = swapChainOut;

        return new RenderingResources
        {
            EngineFactory = _engineFactory,
            ImmediateContext = _immediateContext,
            DeferredContexts = _deferredContexts,
            RenderDevice = _renderDevice,
            SwapChain = _swapChain,
        };
    }
    
    private static void SetMessageCallback(IEngineFactory engineFactory)
    {
        _messageCallback = (severity, message, function, file, line) =>
        {
            CrashOnTDR(message);
            switch (severity)
            {
                case DebugMessageSeverity.Warning:
                case DebugMessageSeverity.Error:
                case DebugMessageSeverity.FatalError:
                    Console.WriteLine($"Diligent Engine: {severity} in {function}() ({file}, {line}): {message}");
                    // Hard exit the process now
                    if (Debugger.IsAttached)
                        Debugger.Break();
                    // Logger.Info("Crashing due to Diligent Engine fatal error.");
                    // Environment.FailFast($"Diligent Engine: {severity} in {function}() ({file}, {line}): {message}");
                    break;
                case DebugMessageSeverity.Info:
                    Console.WriteLine($"Diligent Engine: {severity} {message}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(severity), severity, null);
            }
        };
        _sCallbackRoot = GCHandle.Alloc(_messageCallback, GCHandleType.Normal);
        engineFactory.SetMessageCallback(_messageCallback);
    }

    private static void CrashOnTDR(string message)
    {
        if (!message.Contains(
                "Timeout elapsed while waiting for the frame waitable object. This is a strong indication of a synchronization error."))
            return;
        
        Logger.Error(message);
        throw new Exception("TDR detected");
    }

    private static void CreateRenderDeviceAndSwapChain(
        IEngineFactoryD3D12 engineFactory,
        out IRenderDevice renderDevice,
        out IDeviceContext immediateContext,
        out IDeviceContext[] deferredContexts,
        out ISwapChain swapChain,
        IWindow window)
    {
        engineFactory.CreateDeviceAndContextsD3D12(new EngineD3D12CreateInfo
        {
            #if RELEASE
                EnableValidation = false,
                ValidationFlags = ValidationFlags.None,
                D3D12ValidationFlags = D3D12ValidationFlags.None,
            #elif DEBUG
                EnableValidation = true,
                ValidationFlags = ValidationFlags.None,
                D3D12ValidationFlags = D3D12ValidationFlags.BreakOnCorruption,
            #endif
            NumDeferredContexts = 8
        }, out renderDevice, out IDeviceContext[] contextsOut);
        
        immediateContext = contextsOut[0];
        deferredContexts = contextsOut.Skip(1).ToArray();
        
        swapChain = engineFactory.CreateSwapChainD3D12(
            renderDevice,
            immediateContext,
            new SwapChainDesc()
            {
                BufferCount = 3,
            },
            new FullScreenModeDesc()
            {
                
            },
            new Win32NativeWindow
            {
                Wnd = window.Native!.Win32!.Value.Hwnd
            });
    }

    public void Shutdown()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract - TODO: Diligent kills the swap chain itself?
        if (_swapChain == null)
        {
            Console.Error.WriteLine("SwapChain is already null. Something is wrong :(");
            return;
        }
        _swapChain.Present(0);
        
        AssemblyAssetManager.DisposeAll();
        _swapChain.Dispose();
        _immediateContext.Dispose();
        _renderDevice.Dispose();
    }
}

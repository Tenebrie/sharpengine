using System.Runtime.InteropServices;
using Silk.NET.Windowing;

namespace Engine.Module.Rendering.Platforms;

internal static class MacRuntime
{
    public static unsafe void PrepareInit(ref Native.Bgfx.Bgfx.Init initData, IWindow window)
    {
        var nsWindow = window.Native?.Cocoa ?? throw new InvalidOperationException("No Cocoa window!");
        var contentView = GetContentView(nsWindow);
        var metalLayer = CreateMetalLayer();
        AttachLayerToView(contentView, metalLayer);
            
        initData.type = Native.Bgfx.Bgfx.RendererType.Metal;
        initData.platformData.nwh = metalLayer.ToPointer();
    }
    
    /**
     * Abandon hope all ye who enter here.
     */
    private const string LIBOBJC = "/usr/lib/libobjc.A.dylib";

    [DllImport(LIBOBJC, EntryPoint = "sel_registerName")]
    public static extern IntPtr sel_registerName(string selector);

    [DllImport(LIBOBJC, EntryPoint = "objc_getClass")]
    public static extern IntPtr objc_getClass(string name);

    // Generic message-send that returns an object pointer (e.g. [CAMetalLayer layer])
    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern IntPtr objc_msgSend_IntPtr(IntPtr receiver, IntPtr selector);

    // Message‑send for methods that take an object pointer and return void (e.g. setLayer:)
    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend_Void_IntPtr(IntPtr receiver, IntPtr selector, IntPtr arg);

    // Message‑send for methods that take a bool/BOOL and return void (e.g. setWantsLayer:)
    [DllImport(LIBOBJC, EntryPoint = "objc_msgSend")]
    public static extern void objc_msgSend_Void_Bool(IntPtr receiver, IntPtr selector, bool arg);

    public static IntPtr GetContentView(IntPtr nsWindowPtr)
    {
        var selContentView = sel_registerName("contentView");
        return objc_msgSend_IntPtr(nsWindowPtr, selContentView);
    }

    public static IntPtr CreateMetalLayer()
    {
        var cls = objc_getClass("CAMetalLayer");
        var selLayer = sel_registerName("layer");
        return objc_msgSend_IntPtr(cls, selLayer);
    }

    public static void AttachLayerToView(IntPtr viewPtr, IntPtr layerPtr)
    {
        var selSetLayer    = sel_registerName("setLayer:");
        var selWantsLayer = sel_registerName("setWantsLayer:");

        objc_msgSend_Void_IntPtr(viewPtr, selSetLayer, layerPtr);
        objc_msgSend_Void_Bool   (viewPtr, selWantsLayer, true);
    }
}

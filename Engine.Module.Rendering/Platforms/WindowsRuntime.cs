using Silk.NET.Windowing;

namespace Engine.Module.Rendering.Platforms;

public static class WindowsRuntime
{
    public static unsafe void PrepareInit(ref Native.Bgfx.Bgfx.Init initData, IWindow window)
    {
        initData.type = Native.Bgfx.Bgfx.RendererType.Direct3D11;
        initData.platformData.nwh = (void*)window.Native!.Win32!.Value.Hwnd;
    }
}
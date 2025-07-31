using Silk.NET.Windowing;

namespace Engine.Rendering.Platforms;

public static class WindowsRuntime
{
    public static unsafe void PrepareInit(ref Bindings.Bgfx.Bgfx.Init initData, IWindow window)
    {
        initData.type = Bindings.Bgfx.Bgfx.RendererType.Direct3D11;
        initData.platformData.nwh = (void*)window.Native!.Win32!.Value.Hwnd;
    }
}
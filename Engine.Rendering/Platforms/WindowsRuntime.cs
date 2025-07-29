using Silk.NET.Windowing;

namespace Engine.Rendering.Platforms;

public static class WindowsRuntime
{
    public static unsafe void PrepareInit(ref Codegen.Bgfx.Unsafe.Bgfx.Init initData, IWindow window)
    {
        initData.type = Codegen.Bgfx.Unsafe.Bgfx.RendererType.Direct3D11;
        initData.platformData.nwh = (void*)window.Native!.Win32!.Value.Hwnd;
    }
}
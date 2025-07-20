using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine.Core.Logging;
using JetBrains.Annotations;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;

namespace Engine.Rendering.Bgfx;

public static unsafe class BgfxCallbacks
{
    // === Native function pointer struct (matches bgfx_callback_vtbl_t) ===
    private struct BgfxCallbackVTable
    {
        [UsedImplicitly] public delegate* unmanaged[Cdecl]
            <void*, Fatal, sbyte*, ushort, sbyte*, void> Fatal;

        [UsedImplicitly] public delegate* unmanaged[Cdecl]
            <void*, sbyte*, ushort, sbyte*, void*, void> TraceVargs;

        [UsedImplicitly] public delegate* unmanaged[Cdecl]
            <void*, sbyte*, uint, sbyte*, ushort, void> ProfilerBegin;

        [UsedImplicitly] public delegate* unmanaged[Cdecl]
            <void*, void> ProfilerEnd;

        [UsedImplicitly] public delegate* unmanaged[Cdecl]
            <void*, ulong, uint> CacheReadSize;

        [UsedImplicitly] public delegate* unmanaged[Cdecl]
            <void*, ulong, void*, uint, byte> CacheRead;      // returns byte (bool)

        [UsedImplicitly] public delegate* unmanaged[Cdecl]
            <void*, ulong, void*, uint, void> CacheWrite;

        [UsedImplicitly] public delegate* unmanaged[Cdecl]
            <void*, sbyte*, uint, uint, uint, void*, uint, byte, void> ScreenShot;

        [UsedImplicitly] public delegate* unmanaged[Cdecl]
            <void*, uint, uint, uint, TextureFormat, byte, void> CaptureBegin;

        [UsedImplicitly] public delegate* unmanaged[Cdecl]
            <void*, void> CaptureEnd;

        [UsedImplicitly] public delegate* unmanaged[Cdecl]
            <void*, void*, uint, void> CaptureFrame;
    }

    // Interface object (bgfx_callback_interface) is just a pointer to vtbl:
    private struct BgfxCallbackInterface
    {
        [UsedImplicitly] public BgfxCallbackVTable* Vtbl;
    }

    private static GCHandle _vtblHandle;
    private static GCHandle _ifaceHandle;

    public static IntPtr InterfacePtr { get; private set; } = IntPtr.Zero;

    public static void Install()
    {
        if (InterfacePtr != IntPtr.Zero)
            return;

        // Allocate managed structs (will pin them)
        var vtbl = new BgfxCallbackVTable
        {
            Fatal           = &FatalFn,
            TraceVargs     = &TraceVargsFn,
            ProfilerBegin  = &ProfilerBeginFn,
            ProfilerEnd    = &ProfilerEndFn,
            CacheReadSize = &CacheReadSizeFn,
            CacheRead      = &CacheReadFn,
            CacheWrite     = &CacheWriteFn,
            ScreenShot     = &ScreenShotFn,
            CaptureBegin   = &CaptureBeginFn,
            CaptureEnd     = &CaptureEndFn,
            CaptureFrame   = &CaptureFrameFn
        };

        var iface = new BgfxCallbackInterface
        {
            Vtbl = (BgfxCallbackVTable*)
                GCHandle.Alloc(vtbl, GCHandleType.Pinned).AddrOfPinnedObject()
        };

        _vtblHandle  = GCHandle.Alloc(vtbl,  GCHandleType.Pinned);
        _ifaceHandle = GCHandle.Alloc(iface, GCHandleType.Pinned);

        InterfacePtr = _ifaceHandle.AddrOfPinnedObject();
    }

    public static void Uninstall()
    {
        // Call only *after* bgfx_shutdown.
        if (_ifaceHandle.IsAllocated) _ifaceHandle.Free();
        if (_vtblHandle.IsAllocated)  _vtblHandle.Free();
        InterfacePtr = IntPtr.Zero;
    }

    // === Callback implementations ===

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void FatalFn(void* self, Fatal code, sbyte* file, ushort line, sbyte* msg)
    {
        var f = Marshal.PtrToStringAnsi((nint)file) ?? "<null>";
        var m = Marshal.PtrToStringAnsi((nint)msg)  ?? "<null>";
        Logger.Fatal($"[bgfx fatal] {code} @ {f}:{line} : {m}");
        // You can throw or Environment.FailFast here if desired.
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void TraceVargsFn(void* self, sbyte* file, ushort line, sbyte* fmt, void* args)
    {
        // Minimal: just print format string. (You can add vsnprintf expansion later.)
        var f = Marshal.PtrToStringAnsi((nint)file) ?? "";
        var format = Marshal.PtrToStringAnsi((nint)fmt) ?? "";
        
        Logger.Debug($"[bgfx] {f}({line}): {format}");
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void ProfilerBeginFn(void* self, sbyte* name, uint abgr, sbyte* file, ushort line) { }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void ProfilerEndFn(void* self) { }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static uint CacheReadSizeFn(void* self, ulong id) => 0;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static byte CacheReadFn(void* self, ulong id, void* data, uint size) => 0; // 0 = false

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void CacheWriteFn(void* self, ulong id, void* data, uint size) { }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void ScreenShotFn(void* self, sbyte* filePath,
        uint w, uint h, uint pitch,
        void* data, uint size, byte yflip) { }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void CaptureBeginFn(void* self, uint w, uint h, uint pitch,
        TextureFormat format, byte yflip) { }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void CaptureEndFn(void* self) { }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static void CaptureFrameFn(void* self, void* data, uint size) { }
}
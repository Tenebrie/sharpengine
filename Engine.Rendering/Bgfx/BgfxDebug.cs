using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Engine.Core.Errors;
using Engine.Core.Logging;
using JetBrains.Annotations;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;
// ReSharper disable InconsistentNaming

namespace Engine.Rendering.Bgfx;

internal static unsafe partial class Native
{
    // ---------- vsnprintf ----------
    [LibraryImport("msvcrt", EntryPoint = "vsnprintf")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int vsnprintf_win(
        byte* str, /*size_t*/ UIntPtr size, sbyte* fmt, void* ap);

    [LibraryImport("libc", EntryPoint = "vsnprintf")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int vsnprintf_unix(
        byte* str, UIntPtr size, sbyte* fmt, void* ap);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int vsnprintf(byte* str, UIntPtr size, sbyte* fmt, void* ap)
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? vsnprintf_win(str, size, fmt, ap)
            : vsnprintf_unix(str, size, fmt, ap);

    // ---------- _vscprintf (Windows‑only helper to get length) ----------
    [LibraryImport("msvcrt", EntryPoint = "_vscprintf")]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    private static partial int vscprintf_win(sbyte* fmt, void* ap);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static int vscprintf(sbyte* fmt, void* ap)
        => RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? vscprintf_win(fmt, ap)
            // POSIX: vsnprintf(NULL,0,...) gives required length
            : vsnprintf_unix(null, UIntPtr.Zero, fmt, ap);
}

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
        Logger.Fatal($"BGFX Fatal {code} @ {f}:{line} : {m}");
        Logger.Fatal("BGFX Fatal errors are unrecoverable. Goodbye.");
        KillSwitch.ForceKillProcess(0);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void TraceVargsFn(
        void*  self,
        sbyte* file, ushort line,
        sbyte* fmt,  void*  args)
    {
        // (1) File name
        var fileStr = Marshal.PtrToStringAnsi((nint)file) ?? "<bgfx>";

        // (2) How long will the fully‑formatted string be?
        var len = Native.vscprintf(fmt, args);
        if (len < 0) { Logger.Warn("[bgfx] vsnprintf failed"); return; }

        const int kCap = 16 * 1024;
        len = Math.Min(len, kCap);

        // (3) Format
        if (len <= 512)
        {
            var buf = stackalloc byte[len + 1];
            Native.vsnprintf(buf, (UIntPtr)(len + 1), fmt, args);
            var msg = Marshal.PtrToStringAnsi((nint)buf) ?? "<fmt‑fail>";

            Logger.DebugNoNewline($"{msg}");
            // Logger.DebugNoNewline($"[bgfx] {fileStr}({line}): {msg}");
        }
        else
        {
            var buf = (byte*)Marshal.AllocHGlobal(len + 1);
            Native.vsnprintf(buf, (UIntPtr)(len + 1), fmt, args);
            var msg = Marshal.PtrToStringAnsi((nint)buf) ?? "<fmt‑fail>";

            Marshal.FreeHGlobal((nint)buf);

            Logger.DebugNoNewline($"{msg}");
            // Logger.DebugNoNewline($"[bgfx] {fileStr}({line}): {msg}");
        }
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ProfilerBeginFn(void* self, sbyte* name, uint abgr, sbyte* file, ushort line) { }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ProfilerEndFn(void* self) { }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static uint CacheReadSizeFn(void* self, ulong id) => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte CacheReadFn(void* self, ulong id, void* data, uint size) => 0; // 0 = false

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void CacheWriteFn(void* self, ulong id, void* data, uint size) { }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void ScreenShotFn(void* self, sbyte* filePath,
        uint w, uint h, uint pitch,
        void* data, uint size, byte yflip) { }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void CaptureBeginFn(void* self, uint w, uint h, uint pitch,
        TextureFormat format, byte yflip) { }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void CaptureEndFn(void* self) { }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void CaptureFrameFn(void* self, void* data, uint size) { }
}
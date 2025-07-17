#pragma warning disable 649
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx;
using JetBrains.Annotations;

namespace Engine.Rendering.Bgfx;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static unsafe class BgfxDebug
{
    private struct NativeCallbackStruct
    {
        [UsedImplicitly]
        public delegate* unmanaged[Cdecl]
            <Callback*, Fatal, sbyte*, ushort, sbyte*, void>          OnFatal;
        [UsedImplicitly]
        public delegate* unmanaged[Cdecl]
            <Callback*, sbyte*, ushort, byte*, void*, void>          TraceVargs;
        [UsedImplicitly]
        public delegate* unmanaged[Cdecl] <Callback*, void*, void>    ProfilerBegin;
        [UsedImplicitly]
        public delegate* unmanaged[Cdecl] <Callback*, void>           ProfilerEnd;
        [UsedImplicitly]
        public delegate* unmanaged[Cdecl] <Callback*, void*, Memory*> CacheRead;
        [UsedImplicitly]
        public delegate* unmanaged[Cdecl] <Callback*, void*, Memory*, void> CacheWrite;
        [UsedImplicitly]
        public delegate* unmanaged[Cdecl] <Callback*, TextureHandle, uint, uint, void> ScreenShot;
    }

    private struct Callback
    {
        [UsedImplicitly] public NativeCallbackStruct* Vtbl;
    }

    // ───── FATAL ────────────────────────────────────────────────────────────
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void FatalCallback(Callback* self, Fatal code,
                                      sbyte* file, ushort line,
                                      sbyte* msg)
    {
        Console.WriteLine($"BGFX FATAL [{code}] {Marshal.PtrToStringAnsi((nint)msg)} " +
                          $"@ {Marshal.PtrToStringAnsi((nint)file)}:{line}");
    }

    // ───── TRACE_VARGS ─────────────────────────────────────────────────────
    // use CRT's _vsnprintf to expand the va_list bgfx gives us
    [DllImport("msvcrt", CallingConvention = CallingConvention.Cdecl,
               EntryPoint = "_vsnprintf")]
    private static extern int CRT_vsnprintf(byte* dst, uint size,
                                            byte* fmt, void* arg);

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void TraceCallback(Callback* self,
                                      sbyte* filePath,
                                      ushort line,
                                      byte* fmt,
                                      void* argList)
    {
        const int bufSize = 50000;
        Span<byte> buf = stackalloc byte[bufSize];

        var prefixLen = Encoding.ASCII.GetBytes(
            $"{Marshal.PtrToStringAnsi((nint)filePath)}({line}): ", buf);

        fixed (byte* p = buf)
        {
            var len = CRT_vsnprintf(p + prefixLen,
                                    (uint)(bufSize - prefixLen),
                                    fmt, argList);
            // ReSharper disable once RedundantAssignment
            if (len < 0) len = 0;           // CRT error? show at least the prefix

            Console.WriteLine("[bgfx] " + Encoding.ASCII.GetString(buf[..(prefixLen + len)]));
        }
    }

    // ───── book-keeping ────────────────────────────────────────────────────
    private static GCHandle _cbStructHandle;
    private static GCHandle _cbVTableHandle;

    public static nint CallbackPtr { get; private set; }

    public static void Hook()
    {
        var cbStruct = new Callback();
        var cbVTable = new NativeCallbackStruct
        {
            OnFatal    = &FatalCallback,
            TraceVargs = &TraceCallback
        };

        _cbStructHandle = GCHandle.Alloc(cbStruct,  GCHandleType.Pinned);
        _cbVTableHandle = GCHandle.Alloc(cbVTable, GCHandleType.Pinned);

        var cbPtr = (Callback*)_cbStructHandle.AddrOfPinnedObject();
        cbPtr->Vtbl = (NativeCallbackStruct*)_cbVTableHandle.AddrOfPinnedObject();

        CallbackPtr = (nint)cbPtr;
    }

    public static void Unhook()
    {
        if (_cbStructHandle.IsAllocated) _cbStructHandle.Free();
        if (_cbVTableHandle.IsAllocated) _cbVTableHandle.Free();
    }
}

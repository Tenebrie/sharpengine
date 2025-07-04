#pragma warning disable 649
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static Engine.Codegen.Bgfx.Unsafe.Bgfx; 
using JetBrains.Annotations;

namespace Engine.Rendering.Bgfx;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static unsafe class BgfxDebug
{
    // Order of the callbacks is important, names are not
    private struct NativeCallbackStruct
    {
        [UsedImplicitly] public delegate* unmanaged[Cdecl]<Callback*, Fatal, sbyte*, ushort, sbyte*, void>       OnFatal;
        [UsedImplicitly] public delegate* unmanaged[Cdecl]<Callback*, sbyte*, void*, void>                       OnTrace;
        [UsedImplicitly] public delegate* unmanaged[Cdecl]<Callback*, void*, void>                               ProfilerBegin;
        [UsedImplicitly] public delegate* unmanaged[Cdecl]<Callback*, void>                                      ProfilerEnd;
        [UsedImplicitly] public delegate* unmanaged[Cdecl]<Callback*, void*, Memory*>                            CacheRead;
        [UsedImplicitly] public delegate* unmanaged[Cdecl]<Callback*, void*, Memory*, void>                      CacheWrite;
        [UsedImplicitly] public delegate* unmanaged[Cdecl]<Callback*, TextureHandle, uint, uint, void>           ScreenShot;
    }

    private struct Callback
    {
        [UsedImplicitly] public NativeCallbackStruct* VirtualTable;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void FatalCallback(
        Callback* self, Fatal code,
        sbyte* file, ushort line,
        sbyte* msg)
    {
        Console.WriteLine($"BGFX FATAL [{code}] " +
                          $"{Marshal.PtrToStringAnsi((nint)msg)} " +
                          $"@ {Marshal.PtrToStringAnsi((nint)file)}:{line}");
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void TraceCallback(
        Callback* self, sbyte* fmt,
        void* varargs)
    {
        // Console.WriteLine("[bgfx] " + Marshal.PtrToStringAnsi((nint)fmt));
    }

    private static GCHandle _callbackStructHandle;
    private static GCHandle _callbackVTableHandle;

    public static nint CallbackPtr { get; private set; }

    public static void Hook()
    {
        var callbackStruct = new Callback();
        var callbackVTable = new NativeCallbackStruct
        {
            OnFatal = &FatalCallback,
            OnTrace = &TraceCallback
        };

        // Pin both structs so their addresses never move
        _callbackStructHandle = GCHandle.Alloc(callbackStruct, GCHandleType.Pinned);
        _callbackVTableHandle = GCHandle.Alloc(callbackVTable,  GCHandleType.Pinned);

        var callbackPtr = (Callback*)_callbackStructHandle.AddrOfPinnedObject();
        callbackPtr->VirtualTable = (NativeCallbackStruct*)_callbackVTableHandle.AddrOfPinnedObject();

        CallbackPtr = (nint)(Callback*)_callbackStructHandle.AddrOfPinnedObject();
    }

    public static void Unhook()
    {
        _callbackStructHandle.Free();
        _callbackVTableHandle.Free();
    }
}

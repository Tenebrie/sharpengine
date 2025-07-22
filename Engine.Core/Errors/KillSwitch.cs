using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Engine.Core.Errors;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public static partial class KillSwitch
{
    [LibraryImport("kernel32.dll")]
    private static partial void AddVectoredExceptionHandler(uint first, VectoredHandler handler);

    [StructLayout(LayoutKind.Sequential)]
    private struct EXCEPTION_POINTERS
    {
        public IntPtr ExceptionRecord;
        public IntPtr ContextRecord;
    }

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct EXCEPTION_RECORD
    {
        public uint   ExceptionCode;
        public uint   ExceptionFlags;
        public IntPtr ExceptionRecord;
        public IntPtr ExceptionAddress;
        public uint   NumberParameters;
        public fixed ulong ExceptionInformation[15]; 
    }

    private delegate int VectoredHandler(IntPtr exceptionInfo);

    private static int AvHandler(IntPtr infoPtr)
    {
        unsafe
        {
            var info = (EXCEPTION_POINTERS*)infoPtr;
            var rec  = (EXCEPTION_RECORD*)info->ExceptionRecord;
            var code  = rec->ExceptionCode;
            
            if (code != 0xC0000005)
                return 0;

            Console.WriteLine("Encountered unrecoverable error. Code: 0x{0:X8}", code);
            ForceKillProcess(1);
        }

        return 0;
    }

    private static void PrintManagedStack(int skip = 0)
    {
        var st = new StackTrace(skip, fNeedFileInfo: true);
        var frameMessages = new List<string>();

        foreach (var f in st.GetFrames())
        {
            var method = f.GetMethod();
            var file = f.GetFileName();
            var line  = f.GetFileLineNumber();

            var paramsString = string.Join(", ", f.GetMethod()?.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}") ?? []);
            var methodString = method!.DeclaringType + "." + method.Name + "(" + paramsString + ")";

            var message = file is null
                ? $"   at {methodString}"
                : $"   at {methodString} ({file}:{line})";
            if (!frameMessages.Contains(message))
                frameMessages.Add(message);
        }
        
        foreach (var frameMessage in frameMessages)
        {
            Console.Error.WriteLine(frameMessage);
        }
    }

    public static void InstallAvKiller()
    {
        AddVectoredExceptionHandler(1, AvHandler);
    }

    public static void ForceKillProcess(int skip = 0)
    {
        PrintManagedStack(skip + 2);
        Process.GetCurrentProcess().Kill();
    }
}

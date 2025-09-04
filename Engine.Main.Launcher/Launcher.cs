using System.Diagnostics;

namespace Engine.Main.Launcher;

public static class Launcher
{
    private static void Main()
    {
        if (Directory.Exists("bin") && File.Exists("bin/Engine.Main.Game.exe"))
        {
            RunApp("bin/Engine.Main.Game.exe");
            return;
        }
        if (Directory.Exists("Engine.Main.Editor"))
        {
            RunApp("Engine.Main.Editor/bin/x64/Release/net9.0/Engine.Main.Editor.exe");
            return;
        }

        RunApp("../../../../../Engine.Main.Editor/bin/x64/Release/net9.0/Engine.Main.Editor.exe");
    }

    private static void RunApp(string relativePath)
    {
        var exeFullPath = Path.GetFullPath(relativePath);
        var exeDir  = Path.GetDirectoryName(exeFullPath)!;
        var exeName = Path.GetFileName(exeFullPath);
        
        var process = new ProcessStartInfo(exeFullPath)
        {
            FileName = exeName,
            WorkingDirectory = exeDir,
            UseShellExecute = true
        };
        Process.Start( process );
    }
}

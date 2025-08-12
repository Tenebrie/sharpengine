using System.Diagnostics;

namespace Engine.Core.Shell;

public static class PythonShell
{
    public static void RunEngineScript(string scriptName, string arguments = "")
    {
        Run($"../../../../Scripts/{scriptName}", arguments);
    }
    public static void Run(string scriptPath, string arguments = "")
    {
        return;
        try
        {
            using var process = new Process();
            process.StartInfo.FileName = "python3";
            process.StartInfo.Arguments = $"\"{scriptPath}\" {arguments}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            // Set up asynchronous reading
            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, e) => {
                if (e.Data != null)
                    outputBuilder.AppendLine(e.Data);
            };

            process.ErrorDataReceived += (sender, e) => {
                if (e.Data != null)
                    errorBuilder.AppendLine(e.Data);
            };

            process.Start();
        
            // Begin the asynchronous reading
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
        
            process.WaitForExit();

            Console.WriteLine(outputBuilder);
            Console.Error.WriteLine(errorBuilder);
            
            // Check if the script failed
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"Python script '{scriptPath}' failed with exit code {process.ExitCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error running Python script: {ex.Message}");
            throw;
        }
    }
}
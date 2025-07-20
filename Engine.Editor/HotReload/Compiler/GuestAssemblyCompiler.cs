using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace Engine.Editor.HotReload.Compiler;

public class GuestAssemblyCompiler
{
    public bool IsCompiling = false;
    private readonly Project _project;
    private GuestAssemblyCompiler(string projectPath)
    {
        var pc = ProjectCollection.GlobalProjectCollection;
        _project = pc.LoadProject(projectPath);
        _project.SetProperty("BuildProjectReferences", "false");
    }

    // private static GuestAssemblyCompiler? Instance { get; set; }
    public static GuestAssemblyCompiler Make(string assemblyName)
    {
        var projectPath = Path.GetFullPath($@"..\..\..\..\{assemblyName}\{assemblyName}.csproj");
        return new GuestAssemblyCompiler(projectPath);
    }

    private void Compile()
    {
        var buildParams = new BuildParameters(ProjectCollection.GlobalProjectCollection)
        {
            Loggers = [new ConsoleLogger(LoggerVerbosity.Minimal)]
        };
        
        var request = new BuildRequestData(
            _project.CreateProjectInstance(),    // snapshot of Project at current state
            ["Build"] // targets: Build, Clean, Rebuild, etc.
        );
        
        var result = BuildManager.DefaultBuildManager.Build(buildParams, request);

        Console.WriteLine(result.OverallResult == BuildResultCode.Success
            ? "In-process build succeeded"
            : "Build failed!");
    }

    public Task CompileAsync(Action onSuccess)
    {
        Console.WriteLine("Starting compilation...");
        IsCompiling = true;
        return Task.Run(() =>
        {
            try
            {
                Compile();
                onSuccess.Invoke();
                IsCompiling = false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                IsCompiling = false;
                throw;
            }
        });
    }
}

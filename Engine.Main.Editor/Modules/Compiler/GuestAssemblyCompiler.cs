using Engine.Core.Logging;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace Engine.Main.Editor.HotReload.Compiler;

public class GuestAssemblyCompiler
{
    private readonly string _assemblyName;
    public bool IsCompiling = false;
    private readonly Project _project;
    private GuestAssemblyCompiler(string assemblyName, string projectPath)
    {
        _assemblyName = assemblyName;
        var pc = ProjectCollection.GlobalProjectCollection;
        _project = pc.LoadProject(projectPath);
        _project.SetProperty("BuildProjectReferences", "false");
    }

    // private static GuestAssemblyCompiler? Instance { get; set; }
    public static GuestAssemblyCompiler Make(string assemblyName)
    {
        var projectPath = Path.GetFullPath($@"../../../../{assemblyName}/{assemblyName}.csproj");
        return new GuestAssemblyCompiler(assemblyName, projectPath);
    }

    private void Compile()
    {
        var buildParams = new BuildParameters(ProjectCollection.GlobalProjectCollection)
        {
            Loggers = [new ConsoleLogger(LoggerVerbosity.Minimal)]
        };
        
        var request = new BuildRequestData(
            _project.CreateProjectInstance(),
            ["Build"]
        );
        
        var result = BuildManager.DefaultBuildManager.Build(buildParams, request);

        if (result.OverallResult == BuildResultCode.Failure)
        {
            Logger.ShowPersistent("FailedToCompile",
                "Unable to hot reload assembly, some changes require restarting the editor.");
        }
        else
        {
            Logger.ClearPersistent("FailedToCompile");
        }

        Logger.Debug(result.OverallResult == BuildResultCode.Success
            ? "In-process build succeeded"
            : "Build failed!");
    }

    public Task CompileAsync(Action onSuccess)
    {
        Logger.Info("Starting hot reload for assembly " + _assemblyName);
        IsCompiling = true;
        return Task.Run(() =>
        {
            try
            {
                Compile();
                onSuccess.Invoke();
                IsCompiling = false;
            }
            catch (Exception)
            {
                IsCompiling = false;
                throw;
            }
        });
    }
}

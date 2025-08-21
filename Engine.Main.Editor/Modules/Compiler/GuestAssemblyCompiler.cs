using System.Runtime.CompilerServices;
using Engine.Core.Logging;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace Engine.Main.Editor.Modules.Compiler;

public class GuestAssemblyCompiler
{
    private readonly string _assemblyName;
    public bool IsCompiling = false;
    private readonly Project _project;
    
    private GuestAssemblyCompiler(string assemblyName, string projectPath)
    {
        _assemblyName = assemblyName;
        var globals = new Dictionary<string, string>
        {
            ["Configuration"]  = "Debug",
            ["Platform"]       = "x64",
            ["PlatformTarget"] = "x64",
            ["Prefer32Bit"]    = "false"
        };

        var pc = new ProjectCollection(globals);
        _project = pc.LoadProject(projectPath);
        _project.SetProperty("BuildProjectReferences", "false");
    }

    public static GuestAssemblyCompiler Make(string assemblyName)
    {
        var projectPath = Path.GetFullPath($"../../../../../{assemblyName}/{assemblyName}.csproj");
        return new GuestAssemblyCompiler(assemblyName, projectPath);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Compile(bool filesChanged)
    {
        if (filesChanged)
            _project.MarkDirty();
        var buildParams = new BuildParameters(ProjectCollection.GlobalProjectCollection)
        {
            Loggers = [new ConsoleLogger(LoggerVerbosity.Minimal)]
        };
        
        var request = new BuildRequestData(
            _project.CreateProjectInstance(),
            targetsToBuild: ["Build"]
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

    public Task CompileAsync(bool filesChanged, Action onSuccess)
    {
        Logger.Info("Starting hot reload for assembly " + _assemblyName);
        IsCompiling = true;
        return Task.Run(() =>
        {
            try
            {
                Compile(filesChanged);
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

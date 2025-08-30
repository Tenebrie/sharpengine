using System.Runtime.CompilerServices;
using Engine.Core.Logging;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Graph;
using Microsoft.Build.Logging;

namespace Engine.Main.Editor.Modules.Compiler;

public class GuestAssemblyCompiler
{
    private readonly string _assemblyName;
    public bool IsCompiling = false;
    private readonly Project _project;
    private readonly ProjectCollection _projectCollection;
    private readonly Dictionary<string, string> _globals;
    private readonly BuildParameters _buildParams;
    
    private GuestAssemblyCompiler(string assemblyName, string projectPath)
    {
        _assemblyName = assemblyName;
        _globals = new Dictionary<string, string>
        {
            #if DEBUG
            ["Configuration"]  = "Debug",
            #elif RELEASE
            ["Configuration"]  = "Release",
            #endif
            ["Platform"]       = "x64",
            ["PlatformTarget"] = "x64",
            ["Prefer32Bit"]    = "false"
        };
        
        _projectCollection = new ProjectCollection(_globals);
        _project = _projectCollection.LoadProject(projectPath);
        _buildParams = new BuildParameters(_projectCollection)
        {
            Loggers = [ new ConsoleLogger(LoggerVerbosity.Quiet) ]
        };

        _project.SetProperty("BuildProjectReferences", "false");
    }

    public static GuestAssemblyCompiler Make(string assemblyName)
    {
        var projectPath = Path.GetFullPath($"../../../../../{assemblyName}/{assemblyName}.csproj");
        return new GuestAssemblyCompiler(assemblyName, projectPath);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool Compile(bool filesChanged)
    {
        if (filesChanged)
            _project.MarkDirty();
        
        var graph = new ProjectGraph(_project.FullPath, _globals, _projectCollection);
        var request = new GraphBuildRequestData(graph, ["Build"]);
        var result = BuildManager.DefaultBuildManager.Build(_buildParams, request);

        if (result.OverallResult == BuildResultCode.Failure)
        {
            Logger.ShowPersistent("FailedToCompile",
                "Build failed. Keeping the previous assembly loaded.");
        }
        else
        {
            Logger.ClearPersistent("FailedToCompile");
        }

        var isSuccess = result.OverallResult == BuildResultCode.Success;
        Logger.Debug(isSuccess
            ? "Build succeeded for assembly " + _assemblyName
            : "Build failed for assembly " + _assemblyName);
        return isSuccess;
    }

    public Task CompileAsync(bool filesChanged, Action onSuccess, Action onFinish)
    {
        // Logger.Info("Starting hot reload for assembly " + _assemblyName);
        IsCompiling = true;
        return Task.Run(() =>
        {
            try
            {
                if (Compile(filesChanged))
                    onSuccess.Invoke();
            }
            finally
            {
                IsCompiling = false;
                onFinish.Invoke();
            }
        });
    }
}

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
    private void Compile(bool filesChanged)
    {
        if (filesChanged)
            _project.MarkDirty();
        
        var graph = new ProjectGraph(_project.FullPath, _globals, _projectCollection);
        var request = new GraphBuildRequestData(graph, ["Build"]);
        var result = BuildManager.DefaultBuildManager.Build(_buildParams, request);

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

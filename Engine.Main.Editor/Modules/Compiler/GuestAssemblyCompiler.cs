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
    private Project _project = null!;
    private readonly ProjectCollection _projectCollection;
    private readonly Dictionary<string, string> _globals;
    private readonly BuildParameters _buildParams;
    public bool HasErrors = false;

    private bool _projectLoaded = false;
    private readonly string _projectPath;
    
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
        _buildParams = new BuildParameters(_projectCollection)
        {
            Loggers = [ new ConsoleLogger(LoggerVerbosity.Quiet) ]
        };
        _projectPath = projectPath;
    }

    private void EnsureProjectLoaded()
    {
        if (_projectLoaded)
            return;
        _projectLoaded = true;
        _project = _projectCollection.LoadProject(_projectPath);
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
        Logger.Debug("Compiling assembly " + _assemblyName + " / " + filesChanged);
        if (filesChanged)
            _project.MarkDirty();

        EnsureProjectLoaded();
        
        
        var graph = new ProjectGraph(_project.FullPath, _globals, _projectCollection);
        var request = new GraphBuildRequestData(graph, ["Build"]);
        var result = BuildManager.DefaultBuildManager.Build(_buildParams, request);

        var isSuccess = result.OverallResult == BuildResultCode.Success;
        Logger.Debug(isSuccess
            ? "Build succeeded for assembly " + _assemblyName
            : "Build failed for assembly " + _assemblyName);
        
        return isSuccess;
    }

    public Task CompileAsync(bool filesChanged, Action onSuccess, Action onFinish)
    {
        IsCompiling = true;
        return Task.Run(() =>
        {
            try
            {
                HasErrors = !Compile(filesChanged);
                if (!HasErrors)
                    onSuccess.Invoke();
            }
            catch (Exception ex)
            {
                HasErrors = true;
                Logger.Error("Compile failed: " + ex.Message);
                Console.Error.WriteLine(ex);
                throw;
            }
            finally
            {
                IsCompiling = false;
                onFinish.Invoke();
            }
        });
    }
}

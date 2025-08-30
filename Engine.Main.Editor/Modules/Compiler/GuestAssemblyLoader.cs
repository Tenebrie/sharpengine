using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Engine.Core.Logging;
using Engine.Main.Editor.Modules.Abstract;

namespace Engine.Main.Editor.Modules.Compiler;

internal sealed class GuestAssemblyLoader(string assemblyName)
{
    private readonly string _srcPath = Path.GetFullPath($"../../../../../{assemblyName}");
    private readonly string _dllPath = Path.GetFullPath($"../../../../../{assemblyName}/bin/x64/Debug/net9.0/{assemblyName}.dll");
    private FileSystemWatcher? _watcher;
    private readonly GuestAssemblyCompiler _compiler = GuestAssemblyCompiler.Make(assemblyName);
    public bool IsCompiling => _compiler.IsCompiling;
    private bool _assemblyLoaded = false;
    private bool _isAssemblyDirty = false;
    private bool _isAssemblyStructureDirty = false;
    public bool AssemblyAwaitingReload = false;

    public Assembly? Assembly;
    private GameAssemblyLoadContext? _assemblyLoadContext;

    private double _debounceTimer = 0.0;
    
    /// <summary>
    /// Run a per-frame update, triggering a build if the assembly is dirty.
    /// </summary>
    /// <returns>Whether the assembly needs a reload</returns>
    internal bool Update(double deltaTime)
    {
        if (IsCompiling)
            return false;

        if (!_isAssemblyDirty)
            return AssemblyAwaitingReload;

        if (_debounceTimer > 0.0)
        {
            _debounceTimer -= deltaTime;
            if (_debounceTimer > 0.0)
                return false;
        }
        BuildGuestAsync();
        return false;
    }
    
    private void StartWatching()
    {
        Logger.Debug("Watching for changes in: " + _srcPath);
        _watcher = new FileSystemWatcher(_srcPath, "*.cs")
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };
        _watcher.Changed += OnSourceChangedIncrementally;
        _watcher.Created += OnSourceChanged;
        _watcher.Renamed += OnSourceChanged;
        _watcher.Deleted += OnSourceChanged;
        _watcher.EnableRaisingEvents = true;
    }
    
    private void OnSourceChangedIncrementally(object sender, FileSystemEventArgs e)
    {
        _debounceTimer = 0.05;
        _isAssemblyDirty = true;
        Logger.Debug("Source file updated: " + e.FullPath);
    }

    private void OnSourceChanged(object sender, FileSystemEventArgs e)
    {
        _debounceTimer = 0.05;
        _isAssemblyDirty = true;
        _isAssemblyStructureDirty = true;
        Logger.Debug("Source file changed: " + e.FullPath);
    }

    private static int _assembliesBuilding = 0;
    private static int _assembliesDoneBuilding = 0;

    private Task BuildGuestAsync()
    {
        _isAssemblyDirty = false;
        AssemblyAwaitingReload = false;
        _assembliesBuilding += 1;
        UpdateLoggerState();
        return _compiler.CompileAsync(_isAssemblyStructureDirty, 
        () =>
        {
            AssemblyAwaitingReload = true;
        }, () =>
        {
            _assembliesDoneBuilding += 1;
            if (_assembliesDoneBuilding < _assembliesBuilding)
                return;
            _assembliesBuilding = 0;
            _assembliesDoneBuilding = 0;
            UpdateLoggerState();
        });
    }

    private static void UpdateLoggerState()
    {
        if (_assembliesBuilding == 0)
            Logger.ClearPersistent("AssembliesBuildNotice");
        else
            Logger.ShowPersistent(LogLevel.Warn, "AssembliesBuildNotice", $"Building projects: {_assembliesDoneBuilding} / {_assembliesBuilding}");
    }

    public void LoadAssembly()
    {
        var srcPdb = Path.ChangeExtension(_dllPath, ".pdb");

        var cacheDir = Path.Combine(Path.GetTempPath(), "CustomEngine\\EnginePlugins");
        Directory.CreateDirectory(cacheDir);

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");
        var tmpDll = Path.Combine(cacheDir, $"{assemblyName}_{stamp}.dll");
        var tmpPdb = Path.ChangeExtension(tmpDll, ".pdb");

        File.Copy(_dllPath, tmpDll, overwrite: true);
        if (File.Exists(srcPdb))
            File.Copy(srcPdb, tmpPdb, overwrite: true);

        _assemblyLoaded = true;
        StartWatching();

        _assemblyLoadContext = new GameAssemblyLoadContext(_dllPath, tmpDll);
        Assembly = _assemblyLoadContext.LoadFromAssemblyPath(tmpDll);
    }

    public static void CleanTempFolder()
    {
        var cacheDir = Path.Combine(Path.GetTempPath(), "CustomEngine/EnginePlugins");
        if (!Directory.Exists(cacheDir))
            return;
        Directory.Delete(cacheDir, true);
    }
    
    public TContract? ProduceContract<TContract>() where TContract : class
    {
        if (Assembly == null)
            return null;
        var type = Assembly.GetTypes().Where(ImplementsContract<TContract>).ToList();
        return type.Count == 0 ? null : (TContract)Activator.CreateInstance(type.First())!;
    }

    public void UnloadCurrent()
    {
        if (!_assemblyLoaded)
            return;

        if (_watcher is not null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Changed -= OnSourceChangedIncrementally;
            _watcher.Created -= OnSourceChanged;
            _watcher.Renamed -= OnSourceChanged;
            _watcher.Deleted -= OnSourceChanged;
            _watcher.Dispose();
            _watcher = null;
        }
        
        _assemblyLoadContext!.Unload();
        _assemblyLoadContext = null;
        
        _assemblyLoaded = false;
    }

    private static bool ImplementsContract<TContract>(Type t)
    {
        return t.GetInterfaces().Any(i =>
        {
            if (!i.IsGenericType && i == typeof(TContract))
                return true;
            
            return i.IsGenericType &&
                i.GetGenericTypeDefinition() == typeof(TContract);
        });
    }
}

internal sealed class GameAssemblyLoadContext(string sourceDllPath, string loadedDllPath)
    : AssemblyLoadContext(Path.GetFileNameWithoutExtension(loadedDllPath), isCollectible: true)
{
    private readonly AssemblyDependencyResolver _resolver = new(sourceDllPath);

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name!;

        // Core is already loaded implicitly
        if (name == "Engine.Core")
            return null;

        if (name.StartsWith("User."))
            throw new Exception("User assemblies must not be referenced directly.");
        if (!name.StartsWith("Engine."))
            return LoadExternal(assemblyName);
        var t = AssemblyRepository.LoadLibrary(name);
        if (t.Loader.Assembly == null)
            throw new Exception("Assembly not found: " + name);
        return t.Loader.Assembly;
    }

    private Assembly? LoadExternal(AssemblyName assemblyName)
    {
        var name = assemblyName.Name!;
        if (AssemblyRepository.ExternalAssemblies.TryGetValue(name, out var externalAssembly))
            return externalAssembly;
        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        if (path == null)
            return null;

        var newAssembly = LoadFromAssemblyPath(path);
        AssemblyRepository.ExternalAssemblies[name] = newAssembly;
        return newAssembly;
    }
}
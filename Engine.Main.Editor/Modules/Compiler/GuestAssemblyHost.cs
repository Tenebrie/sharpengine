using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Engine.Core.Logging;

namespace Engine.Main.Editor.Modules.Compiler;

internal sealed class GuestAssemblyHost(string assemblyName)
{
    private readonly string _srcPath = Path.GetFullPath($"../../../../../{assemblyName}");
    private readonly string _dllPath = Path.GetFullPath($"../../../../../{assemblyName}/bin/x64/Debug/net9.0/{assemblyName}.dll");
    private FileSystemWatcher? _watcher;
    private readonly GuestAssemblyCompiler _compiler = GuestAssemblyCompiler.Make(assemblyName);
    private bool IsCompiling => _compiler.IsCompiling;
    private bool _assemblyLoaded = false;
    private bool _isAssemblyDirty = false;
    private bool _isAssemblyStructureDirty = false;
    public bool AssemblyAwaitingReload = false;

    public Assembly? Assembly;
    private GameAssemblyLoadContext? _assemblyLoadContext;

    /// <summary>
    /// Run a per-frame update, triggering a build if the assembly is dirty.
    /// </summary>
    /// <returns>Whether the assembly needs a reload</returns>
    internal bool Update()
    {
        if (IsCompiling)
            return false;

        if (!_isAssemblyDirty)
            return AssemblyAwaitingReload;

        BuildGuestAsync();
        return false;
    }

    private void StartWatching()
    {
        Logger.Debug("Watching for changes in: " + _srcPath);
        _watcher = new FileSystemWatcher(
                _srcPath,
                "*.cs")
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
        _isAssemblyDirty = true;
        Logger.Debug("Source file updated: " + e.FullPath);
    }

    private void OnSourceChanged(object sender, FileSystemEventArgs e)
    {
        _isAssemblyDirty = true;
        _isAssemblyStructureDirty = true;
        Logger.Debug("Source file changed: " + e.FullPath);
    }

    private void BuildGuestAsync()
    {
        _isAssemblyDirty = false;
        AssemblyAwaitingReload = false;
        _compiler.CompileAsync(_isAssemblyStructureDirty, () =>
        {
            AssemblyAwaitingReload = true;
        });
    }

    /* ---------- internals ---------- */

    public TContract? LoadAssembly<TContract>() where TContract : class
    {
        var srcPdb = Path.ChangeExtension(_dllPath, ".pdb");

        var cacheDir = Path.Combine(Path.GetTempPath(), "CustomEngine/EnginePlugins");
        Directory.CreateDirectory(cacheDir);

        var stamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");
        var tmpDll = Path.Combine(cacheDir, $"{assemblyName}_{stamp}.dll");
        var tmpPdb = Path.ChangeExtension(tmpDll, ".pdb");

        File.Copy(_dllPath, tmpDll, overwrite: true);
        if (File.Exists(srcPdb))
            File.Copy(srcPdb, tmpPdb, overwrite: true);

        _assemblyLoaded = true;
        StartWatching();

        _assemblyLoadContext = new GameAssemblyLoadContext(tmpDll);
        Assembly = _assemblyLoadContext.LoadFromAssemblyPath(tmpDll);

        var type = Assembly.GetTypes().Where(ImplementsContract<TContract>).ToList();
        return type.Count == 0 ? null : (TContract)Activator.CreateInstance(type.First())!;
    }
    
    public TContract? LoadContract<TContract>() where TContract : class
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

using Engine.User.Contracts;
using Engine.Worlds.Entities;

namespace Engine.Editor.HotReload.Compiler;

using EngineSettings = IEngineContract<Backstage>;

internal sealed class GuestAssemblyHost(string assemblyName)
{
    private readonly string _srcPath = Path.GetFullPath($@"..\..\..\..\{assemblyName}");
    private readonly string _dllPath = Path.GetFullPath($@"..\..\..\..\{assemblyName}\bin\Debug\net9.0\{assemblyName}.dll");
    private FileSystemWatcher? _watcher;
    private readonly GuestAssemblyCompiler _compiler = GuestAssemblyCompiler.Make(assemblyName);
    public bool IsCompiling => _compiler.IsCompiling;
    public bool AssemblyLoaded = false;
    public bool IsAssemblyDirty = false;
    public bool AssemblyAwaitingReload = false;

    private GameAssemblyLoadContext? _assemblyLoadContext;

    /// <summary>
    /// Run a per-frame update, triggering a build if the assembly is dirty.
    /// </summary>
    /// <returns>Whether the assembly needs a reload</returns>
    internal bool Update()
    {
        if (IsCompiling)
            return false;

        if (!IsAssemblyDirty)
            return AssemblyAwaitingReload;
        
        BuildGuestAsync();
        return false;

    }
    
    private void StartWatching()
    {
        Console.WriteLine("Watching for changes in: " + _srcPath);
        _watcher = new FileSystemWatcher(
                _srcPath,
                "*.cs")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
            };
        _watcher.Changed += OnSourceChanged;
        _watcher.Created += OnSourceChanged;
        _watcher.Renamed += OnSourceChanged;
        _watcher.EnableRaisingEvents = true;
    }
    
    private void OnSourceChanged(object sender, FileSystemEventArgs e)
    {
        IsAssemblyDirty = true;
        Console.WriteLine("Source file changed: " + e.FullPath);
    }
    
    public void BuildGuestAsync()
    {
        IsAssemblyDirty = false;
        AssemblyAwaitingReload = false;
        _compiler.CompileAsync(() =>
        {
            AssemblyAwaitingReload = true;
            
        });
    }

    /* ---------- internals ---------- */

    public TContract? Load<TContract>() where TContract : class
    {
        var srcPdb = Path.ChangeExtension(_dllPath, ".pdb");

        var cacheDir = Path.Combine(Path.GetTempPath(), "CustomEngine/EnginePlugins");
        Directory.CreateDirectory(cacheDir);

        var stamp   = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");
        var tmpDll  = Path.Combine(cacheDir, $"{assemblyName}_{stamp}.dll");
        var tmpPdb  = Path.ChangeExtension(tmpDll, ".pdb");

        File.Copy(_dllPath, tmpDll, overwrite: true);
        if (File.Exists(srcPdb))
            File.Copy(srcPdb, tmpPdb, overwrite: true);

        AssemblyLoaded = true;
        StartWatching();
        
        _assemblyLoadContext = new GameAssemblyLoadContext(tmpDll);
        var asm = _assemblyLoadContext.LoadFromAssemblyPath(tmpDll);

        var type = asm.GetTypes().Where(ImplementsContract<TContract>).ToList();
        return type.Count == 0 ? null : (TContract)Activator.CreateInstance(type.First())!;
    }

    public void UnloadCurrent()
    {
        if (!AssemblyLoaded)
            return;
        // try { _entry?.Dispose(); } catch { /* ignore */ }
        // _entry = null;

        _assemblyLoadContext?.Unload();
        _assemblyLoadContext = null;

        GC.Collect();
        GC.WaitForPendingFinalizers();
        AssemblyLoaded = false;
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

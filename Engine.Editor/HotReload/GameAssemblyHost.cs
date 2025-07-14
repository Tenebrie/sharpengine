using Engine.User.Contracts;
using Engine.Worlds.Entities;

namespace Engine.Editor.HotReload;

using EngineSettings = IEngineSettings<Backstage>;

internal sealed class GameAssemblyHost(string assemblyName)
{
    private readonly string _srcPath = Path.GetFullPath($@"..\..\..\..\{assemblyName}");
    private readonly string _dllPath = Path.GetFullPath($@"..\..\..\..\{assemblyName}\bin\Debug\net9.0\{assemblyName}.dll");
    private FileSystemWatcher? _watcher;
    private readonly GameAssemblyCompiler _compiler = GameAssemblyCompiler.GetInstance(assemblyName);
    public bool IsCompiling => _compiler.IsCompiling;
    public bool AssemblyLoaded = false;
    public bool IsAssemblyDirty = false;
    public bool AssemblyAwaitingReload = false;

    private GameAssemblyLoadContext? _assemblyLoadContext;

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

    public EngineSettings Load()
    {
        var srcPdb = Path.ChangeExtension(_dllPath, ".pdb");

        var cacheDir = Path.Combine(Path.GetTempPath(), "CustomEngine/EnginePlugins");
        Directory.CreateDirectory(cacheDir);

        var stamp   = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff");
        var tmpDll  = Path.Combine(cacheDir, $"User.Game_{stamp}.dll");
        var tmpPdb  = Path.ChangeExtension(tmpDll, ".pdb");

        File.Copy(_dllPath, tmpDll, overwrite: true);
        if (File.Exists(srcPdb))
            File.Copy(srcPdb, tmpPdb, overwrite: true);
        
        _assemblyLoadContext = new GameAssemblyLoadContext(tmpDll);
        var asm = _assemblyLoadContext.LoadFromAssemblyPath(tmpDll);

        var type = asm.GetTypes().Where(ImplementsEngineSettings).ToList();
        if (type.Count == 0)
        {
            throw new Exception("No engine settings found");
        }
        var userSettings  = (EngineSettings)Activator.CreateInstance(type.First())!;

        AssemblyLoaded = true;
        StartWatching();
        return userSettings;
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

    private static bool ImplementsEngineSettings(Type t) =>
        t.GetInterfaces().Any(i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IEngineSettings<>));
}

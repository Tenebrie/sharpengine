using Engine.Core.Logging;
using Engine.Main.Editor.Modules.Compiler;

namespace Engine.Main.Editor.Modules.Abstract;

public class LibraryAssembly(string assemblyName)
{
    public string Name => assemblyName;
    internal GuestAssemblyLoader Loader { get; } = new(assemblyName);
    internal HashSet<string> Dependencies { get; } = [];
    internal int ReloadPriority { get; set; } = 0;
    internal bool SkipNextUpdate = false;

    public virtual void Load()
    {
        Loader.UnloadCurrent();
        Logger.Info("Loading " + assemblyName);
        Loader.AssemblyAwaitingReload = false;
        Loader.LoadAssembly();
        
        if (Loader.Assembly is null)
            return;
        
        foreach (var referencedAssembly in Loader.Assembly.GetReferencedAssemblies().Where(a => a.FullName.StartsWith("Engine.") || a.FullName.StartsWith("User.")))
            Dependencies.Add(referencedAssembly.Name!);
        AssemblyRepository.RegisterAssemblyDeps(assemblyName, Dependencies.ToList());
        Logger.Info("Done loading " + assemblyName);
    }
    
    public virtual bool Update(double deltaTime)
    {
        if (!SkipNextUpdate)
            return Loader.Update(deltaTime);
        
        SkipNextUpdate = false;
        return false;
    }
    
    public bool NeedsReload() => Loader.AssemblyAwaitingReload;

    public void QueueReload()
    {
        Logger.Info("QUEUE RELOAD");
        Loader.AssemblyAwaitingReload = true;
    }

    public void Reload()
    {
        Unload();
        AssemblyRepository.InvalidateDependencies(assemblyName);
        Load();
    }

    public virtual void Unload()
    {
        Logger.Info("Unloading " + assemblyName);
        Loader.AssemblyAwaitingReload = false;
        
    }
}
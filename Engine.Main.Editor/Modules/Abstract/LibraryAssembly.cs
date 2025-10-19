using Engine.Main.Editor.Modules.Compiler;

namespace Engine.Main.Editor.Modules.Abstract;

public class LibraryAssembly(string assemblyName)
{
    public string Name => assemblyName;
    internal GuestAssemblyLoader Loader { get; } = new(assemblyName);
    internal HashSet<string> Dependencies { get; } = [];
    internal int ReloadPriority { get; set; } = 0;
    internal virtual int ImplicitReloadPriority { get; set; } = 0;

    public virtual void Load()
    {
        Loader.AssemblyAwaitingReload = false;
        Loader.LoadAssembly();
        
        if (Loader.Assembly is null)
            return;
        
        foreach (var referencedAssembly in Loader.Assembly.GetReferencedAssemblies().Where(a => a.FullName.StartsWith("Engine.") || a.FullName.StartsWith("User.")))
            Dependencies.Add(referencedAssembly.Name!);
        AssemblyRepository.RegisterAssemblyDeps(assemblyName, Dependencies.ToList());
    }
    
    public virtual void Update(double deltaTime)
    {
        Loader.Update(deltaTime);
    }
    
    public bool NeedsRebuild() => Loader.IsAssemblyDirty;
    public bool NeedsReload() => Loader.AssemblyAwaitingReload;

    public void QueueReload()
    {
        Loader.AssemblyAwaitingReload = true;
    }

    public void Reload()
    {
        Unload();
        Load();
    }

    public virtual void Unload()
    {
        Loader.AssemblyAwaitingReload = false;
        Loader.UnloadCurrent();
    }
}
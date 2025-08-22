using Engine.Core.Communication.Tasks;
using Engine.Core.Contracts;
using Engine.Core.Modules;
using Engine.Main.Editor.Modules.Compiler;

namespace Engine.Main.Editor.Modules.Abstract;

public abstract class ModularAssembly(string assemblyName, EngineModule module)
{
    internal EngineModule Module => module;

    internal double TimeScale = 1.0;
    
    internal GuestAssemblyLoader Loader { get; } = new(assemblyName);

    internal abstract IModularHost? GetHost();

    public virtual void Load() { }

    public virtual bool Update(double deltaTime)
    {
        return Loader.Update();
    }

    public void Reload()
    {
        Loader.AssemblyAwaitingReload = false;
        Unload();
        Load();
    }

    protected virtual void Unload()
    {
        if (Loader.Assembly is not null)
            MainThreadTask.Purge(Loader.Assembly);
        Loader.UnloadCurrent();
    }
    
    public virtual void Destroy()
    {
        Unload();
    }
}
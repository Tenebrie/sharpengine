﻿using Engine.Core.EntitySystem.Entities;
using Engine.Core.Contracts;
using Engine.Main.Editor.HotReload.Compiler;

namespace Engine.Main.Editor.Modules.Abstract;

public abstract class GuestAssembly(string assemblyName)
{
    internal string AssemblyName = assemblyName;
    internal GuestAssemblyHost Host { get; } = new(assemblyName);
    
    internal Backstage? Backstage { get; set; }
    internal IEngineContract<Backstage>? Settings { get; set; }
    
    public virtual void Init() {}

    public virtual bool Update(double deltaTime)
    {
        return Host.Update();
    }

    public void Reload()
    {
        Host.AssemblyAwaitingReload = false;
        Destroy();
        Backstage?.FreeImmediately();

        Host.UnloadCurrent();
        Settings = null;
        Backstage = null;
        Init();
    }
    
    protected virtual void Destroy() {}
}
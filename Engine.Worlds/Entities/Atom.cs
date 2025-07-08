using System.ComponentModel;
using System.Reflection;
using Engine.Worlds.Attributes;

namespace Engine.Worlds.Entities;

public class Atom
{
    internal Atom() {}

    public Backstage? Backstage { get; internal set; }
    public Atom Parent { get; internal set; } = null!;
    public List<Atom> Children { get; } = [];
    
    protected T RegisterAtom<T>(T atom) where T : Atom, new()
    {
        Children.Add(atom);
        atom.Parent = this;
        atom.Backstage = Backstage;
        atom.InitializeLifecycle();
        return atom;
    }

    public bool IsTicking { get; set; }
    public Action? OnInitCallback { get; set; }
    public Action<double>? OnUpdateCallback { get; set; }
    public Action? OnDestroyCallback { get; set; }
    internal void InitializeLifecycle()
    {
        var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        var initMethods = methods.Where(method => method.GetCustomAttribute<OnInit>() != null).ToList();
        foreach (var action in initMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action), this, methodInfo)))
        {
            OnInitCallback += (Action)action;
        }

        var updateMethods = methods.Where(method => method.GetCustomAttribute<OnUpdate>() != null).ToList();
        foreach (var action in updateMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action<double>), this, methodInfo)))
        {
            OnUpdateCallback += (Action<double>)action;
        }
        IsTicking = updateMethods.Count > 0;

        var destroyMethods = methods.Where(method => method.GetCustomAttribute<OnDestroy>() != null).ToList();
        foreach (var action in destroyMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action), this, methodInfo)))
        {
            OnDestroyCallback += (Action)action;
        }
        
        OnInitCallback?.Invoke();
    }
    
    internal void ProcessLogicFrame(double deltaTime)
    {
        if (IsTicking)
        {
            OnUpdateCallback?.Invoke(deltaTime);
        }
        
        Children.ForEach(child => child.ProcessLogicFrame(deltaTime));
    }
    
    public void Free()
    {
        Children.ForEach(child => child.Free());
        OnDestroyCallback?.Invoke();
        if (Backstage == null) return;
        
        Parent.Children.Remove(this);
        Backstage = null;
    }

    public bool IsBeingDestroyed { get; internal set; } = false;
    public void QueueFree()
    {
        IsBeingDestroyed = true;
    }
}

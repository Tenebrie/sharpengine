using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Engine.Worlds.Attributes;
using Engine.Worlds.Services;

namespace Engine.Worlds.Entities;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
public partial class Atom
{
    public Action? OnInitCallback { get; set; }
    
    public bool IsTicking { get; set; }
    public Action<double>? OnUpdateCallback { get; set; }
    
    public Action? OnDestroyCallback { get; set; }
    
    private void InitializeLifecycle()
    {
        var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        var initMethods = methods.Where(method => method.GetCustomAttribute<OnInitAttribute>() != null).ToList();
        foreach (var action in initMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action), this, methodInfo)))
        {
            OnInitCallback += (Action)action;
        }

        var updateMethods = methods.Where(method => method.GetCustomAttribute<OnUpdateAttribute>() != null).ToArray();
        var simpleUpdateMethods = updateMethods.Where(method => method.GetParameters().Length == 0).ToList();
        var properUpdateMethods = updateMethods.Where(method => method.GetParameters().Length > 0).ToList();
        foreach (var action in simpleUpdateMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action), this, methodInfo)))
        {
            Console.WriteLine("Adding simple update method: " + action.Method.Name);
            OnUpdateCallback += _ => ((Action)action)();
        }
        foreach (var action in properUpdateMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action<double>), this, methodInfo)))
        {
            OnUpdateCallback += (Action<double>)action;
        }
        IsTicking = updateMethods.Length != 0;

        var destroyMethods = methods.Where(method => method.GetCustomAttribute<OnDestroyAttribute>() != null).ToList();
        foreach (var action in destroyMethods.Select(methodInfo => Delegate.CreateDelegate(typeof(Action), this, methodInfo)))
        {
            OnDestroyCallback += (Action)action;
        }
        
        OnInitCallback?.Invoke();
    }
    
    protected internal void ProcessLogicFrame(double deltaTime)
    {
        if (IsTicking)
        {
            OnUpdateCallback?.Invoke(deltaTime);
        }
        
        var children = Children.ToList(); // Create a copy to avoid modification issues during iteration
        children.ForEach(child => child.ProcessLogicFrame(deltaTime));
    }
    
    public void FreeImmediately()
    {
        var childrenCount = Children.Count;
        while (childrenCount > 0)
        {
            Children[0].FreeImmediately();
            if (Children.Count >= childrenCount)
                throw new InvalidOperationException("Child count did not decrease after FreeImmediately call.");
            childrenCount = Children.Count;
        }
        OnDestroyCallback?.Invoke();
        if (Parent == null) return;
        
        Parent.Children.Remove(this);
        Backstage = null!;
    }

    public bool IsBeingDestroyed { get; internal set; }
    public void QueueFree()
    {
        IsBeingDestroyed = true;
        FindService<ReaperService>().Condemn(this);
    }
}

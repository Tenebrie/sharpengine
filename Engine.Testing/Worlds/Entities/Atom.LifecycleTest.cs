using Engine.Worlds.Attributes;
using Engine.Worlds.Entities;
using Engine.Worlds.Entities.BuiltIns;
using Engine.Worlds.Services;

namespace Engine.Testing.Worlds.Entities;

public class AtomLifecycleTest
{
    [Fact]
    public void AtomLifecycleTest_PassesOnInitToChildren()
    {
        var parent = new StandaloneAtom();
        var child = new MockAtom();
        
        parent.AdoptChild(child);
        
        Assert.Equal(1, child.OnInitCallCount);
    }
    
    [Fact]
    public void AtomLifecycleTest_PassesOnUpdateToChildren()
    {
        var parent = new StandaloneAtom();
        var child = new MockAtom();
        parent.AdoptChild(child);
        parent.ProcessLogicFrame(3.14);
        
        Assert.Equal(3.14, child.TotalDeltaTime);
        Assert.Equal(1, child.OnSimpleUpdateCallCount);
        Assert.Equal(1, child.OnComplexUpdateCallCount);
    }
    
    [Fact]
    public void AtomLifecycleTest_PassesOnDestroyToChildren()
    {
        var parent = new StandaloneAtom();
        var child = new MockAtom();
        parent.AdoptChild(child);
        
        List<Atom> destructionOrder = [];
        parent.OnDestroyCallback += () => destructionOrder.Add(parent);
        child.OnDestroyCallback += () => destructionOrder.Add(child);
        parent.FreeImmediately();
        
        Assert.Equal(1, child.OnDestroyCallCount);
        // Child should be destroyed before parent
        Assert.Equal(destructionOrder[0], child);
        Assert.Equal(destructionOrder[1], parent);
    }
    
    [Fact]
    public void AtomLifecycleTest_QueueFreeMarksAtomForDestruction()
    {
        var backstage = new Backstage();
        var parent = new MockAtom();
        var child = new MockAtom();
        backstage.AdoptChild(parent);
        parent.AdoptChild(child);
        parent.QueueFree();
        
        Assert.Equal(0, parent.OnDestroyCallCount);
        Assert.Equal(0, child.OnDestroyCallCount);
        Assert.True(parent.IsBeingDestroyed, "Parent should be marked for destruction.");
        Assert.False(child.IsBeingDestroyed, "Child should not be marked for destruction.");
    }
    
    [Fact]
    public void AtomLifecycleTest_QueueFreeCondemnsAtom()
    {
        var backstage = new Backstage();
        var parent = new MockAtom();
        var child = new MockAtom();
        backstage.AdoptChild(parent);
        parent.AdoptChild(child);
        parent.QueueFree();
        
        Assert.Single(backstage.GetService<ReaperService>().CondemnedAtoms);
        Assert.Equal(parent, backstage.GetService<ReaperService>().CondemnedAtoms[0]);
    }
}

internal class MockAtom : Atom
{
    public int OnInitCallCount { get; private set; }
    public int OnSimpleUpdateCallCount { get; private set; }
    public int OnComplexUpdateCallCount { get; private set; }
    public int OnDestroyCallCount { get; private set; }
    
    public double TotalDeltaTime { get; private set; }

    [OnInit]
    internal void OnInit() => OnInitCallCount++;
    [OnUpdate]
    internal void OnSimpleUpdate() => OnSimpleUpdateCallCount++;
    [OnUpdate]
    internal void OnComplexUpdate(double deltaTime)
    {
        OnComplexUpdateCallCount++;
        TotalDeltaTime += deltaTime;
    }

    [OnDestroy]
    internal void OnDestroy() => OnDestroyCallCount++;
}
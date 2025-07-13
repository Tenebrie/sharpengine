using System.Diagnostics.CodeAnalysis;

namespace Engine.Worlds.Entities;

[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "MemberCanBeProtected.Global")]
public partial class Atom
{
    public Backstage Backstage { get; internal set; } = null!;
    public Atom? Parent { get; internal set; }
    public List<Atom> Children { get; } = [];

    private bool _isInitialized = false;
    internal void Initialize()
    {
        InitializeComponents();
        InitializeLifecycle();
        InitializeInput();
        InitializeChildren();
        
        _isInitialized = true;
    }
    
    public T AdoptChild<T>(T atom) where T : Atom, new()
    {
        Children.Add(atom);
        atom.Parent = this;
        if (this is Backstage backstage)
            atom.Backstage = backstage;
        else
            atom.Backstage = Backstage;
        if (_isInitialized)
            atom.Initialize();
        return atom;
    }

    public T GetService<T>() where T : Service, new()
    {
        if (Backstage == null)
            throw new InvalidOperationException("Atom is not registered in a Backstage.");
        return Backstage.ServiceRegistry.Get<T>();
    }
    
    public static bool IsValid(Atom? atom)
    {
        return atom is { IsBeingDestroyed: false } && IsValid(atom.Backstage);
    }
}

using Engine.Core.EntitySystem.Entities;

namespace Engine.Core.EntitySystem.Services;

public partial class ReaperService : Service
{
    public List<Atom> CondemnedAtoms { get; } = [];
    
    public void Condemn(Atom atom)
    {
        CondemnedAtoms.Add(atom);
    }
    
    public void Reap()
    {
        // ReSharper disable once ForCanBeConvertedToForeach
        for (var i = 0; i < CondemnedAtoms.Count; i++)
        {
            var atom = CondemnedAtoms[i];
            if (IsStale(atom))
                continue;
                
            atom.FreeImmediately();
        }
        CondemnedAtoms.Clear();
    }
}
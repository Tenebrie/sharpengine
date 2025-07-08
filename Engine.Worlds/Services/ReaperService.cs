using Engine.Worlds.Entities;

namespace Engine.Worlds.Services;

public class ReaperService : Service
{
    public List<Atom> CondemnedAtoms { get; } = [];
    
    public void Condemn(Atom atom)
    {
        CondemnedAtoms.Add(atom);
    }
    
    public void Reap()
    {
        foreach (var atom in CondemnedAtoms.Where(a => a.Backstage != null))
        {
            atom.FreeImmediately();
        }
        CondemnedAtoms.Clear();
    }
}
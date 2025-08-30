using System.Collections;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;

namespace Engine.Core.Communication.Groups;

public interface IGroup
{
    public void Join(object member);
    public void Leave(object member);
}

[MeansImplicitUse]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public class Group<T> : IGroup, IEnumerable<T> where T : class
{
    private List<T> Members { get; } = [];

    private T? First => Members.Count > 0 ? Members[0] : null;
    
    private void Join(T member)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (Members.Contains(member))
            return;

        Members.Add(member);
    }

    public void Join(object member) => Join((T)member);
    
    private void Leave(T member)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (!Members.Contains(member))
            return;
        
        Members.Remove(member);
    }
    public void Leave(object member) => Leave((T)member);

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var member in Members)
        {
            yield return member;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
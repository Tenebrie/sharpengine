using System.Collections;
using Engine.Core.Logging;
using JetBrains.Annotations;

namespace Engine.Core.Communication.Groups;

public interface IGroup
{
    public void Join(object member);
    public void Leave(object member);
}

[MeansImplicitUse]
public class Group<T> : IGroup, IEnumerable<T> where T : class
{
    private readonly List<T> _members = [];
    public List<T> Members => _members;
    
    private void Join(T member)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (_members.Contains(member))
            return;

        _members.Add(member);
    }

    public void Join(object member) => Join((T)member);
    
    private void Leave(T member)
    {
        ArgumentNullException.ThrowIfNull(member);

        if (!_members.Contains(member))
            return;
        
        Logger.InfoF("Removing member to group: {0}", member.GetType().Name);

        _members.Remove(member);
    }
    public void Leave(object member) => Leave((T)member);

    public IEnumerator<T> GetEnumerator()
    {
        foreach (var member in _members)
        {
            yield return member;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
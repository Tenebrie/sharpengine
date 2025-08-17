using Engine.Core.Communication.Groups;
using Engine.Core.EntitySystem.Attributes;

namespace Engine.Core.EntitySystem.Entities;

public partial class Atom
{
    private readonly List<IGroup> _groupMemberships = [];
    
    private void InitializeGroups()
    {
        // Instance groups
        var data = ReflectionDataCache.GetValueOrDefault(GetType());
        var instanceGroupList = data.DefaultGroupFields.Where(reflection => !reflection.IsStatic).ToList();
        var staticGroupList = data.DefaultGroupFields.Where(reflection => reflection.IsStatic).ToList();
        
        foreach (var reflection in instanceGroupList)
        {
            var group = reflection.GetValue(this);
            if (group is null)
            {
                group = reflection.Factory();
                reflection.SetValue(this, group);
            }

            group.Join(this);
            _groupMemberships.Add(group);
        }
        
        // Static groups
        foreach (var reflection in staticGroupList)
        {
            var group = reflection.GetValue(null);
            if (group is null)
                throw new Exception("Static group field " + reflection.FieldInfo.Name + " is null. Ensure it is initialized properly.");
            
            group.Join(this);
            _groupMemberships.Add(group);
        }
    }

    [OnDestroy]
    protected void OnClearGroupMemberships()
    {
        foreach (var group in _groupMemberships)
            group.Leave(this);
    }
}

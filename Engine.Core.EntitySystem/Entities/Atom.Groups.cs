using System.Collections.Immutable;
using System.Reflection;
using Engine.Core.Communication.Groups;
using Engine.Core.Communication.Signals;
using Engine.Core.EntitySystem.Attributes;

namespace Engine.Core.EntitySystem.Entities;

public partial class Atom
{
    private readonly List<IGroup> _groupMemberships = [];
    
    private void InitializeGroups()
    {
        // Instance groups
        
        var fields = GetType().GetFields(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var componentFields = fields
            .Where(method => !method.IsStatic && method.GetCustomAttributes<DefaultGroupAttribute>().Any())
            .ToList();
        
        foreach (var field in componentFields)
        {
            IGroup group;
            var value = field.GetValue(this);
            if (value is null)
            {
                group = CreateGroupInstance(field.FieldType);
                field.SetValue(this, group);
            }
            else
            {
                group = (IGroup)value;
            }

            group.Join(this);
            _groupMemberships.Add(group);
        }
        
        // Static groups
        
        fields = GetType().GetFields(
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        componentFields = fields
            .Where(method => method.IsStatic && method.GetCustomAttributes<DefaultGroupAttribute>().Any())
            .ToList();
        foreach (var field in componentFields)
        {
            var group = (IGroup?)field.GetValue(null);
            if (group is null)
                throw new Exception("Static group field " + field.Name + " is null. Ensure it is initialized properly.");
            
            group.Join(this);
            _groupMemberships.Add(group);
        }
    }

    private static IGroup CreateGroupInstance(Type type)
    {
        if (type is not { IsClass: true })
            throw new Exception("Type " + type.Name + " is not a valid signal type (groups must be classes).");
        
        if (type is not { IsAbstract: false })
            throw new Exception("Type " + type.Name + " is not a valid signal type (groups must not be abstract).");
        
        var constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
            throw new Exception("Type " + type.Name + " is not a valid signal type (groups must have a parameterless constructor).");
        
        var newInstance = Activator.CreateInstance(type);
        if (newInstance is not IGroup group)
            throw new Exception("Type " + type.Name + " is not a valid signal type (groups must implement IGroup or inherit from Group).");
        
        return group;
    }

    [OnDestroy]
    protected void OnClearGroupMemberships()
    {
        foreach (var group in _groupMemberships)
            group.Leave(this);
    }
}

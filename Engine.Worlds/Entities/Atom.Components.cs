using System.Reflection;
using Engine.Worlds.Attributes;

namespace Engine.Worlds.Entities;

public partial class Atom
{
    private void InitializeComponents()
    {
        var fields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        var componentFields = fields.Where(method => method.GetCustomAttributes<ComponentAttribute>().Any()).ToList();
        
        foreach (var field in componentFields.Where(field => field.GetValue(this) == null))
        {
            var component = CreateDefaultInstance(field.FieldType);
            field.SetValue(this, component);
            AdoptChild(component);
            if (this is Actor && component is ActorComponent actorComponent)
            {
                actorComponent.Actor = (Actor)this;
            }
        }
    }
    
    private void InitializeChildren()
    {
        foreach (var child in Children)
            child.Initialize();
    }
    
    public T? GetParent<T>() where T : Atom
    {
        var parent = Parent;
        while (parent != null)
        {
            if (parent is T t)
                return t;
            parent = parent.Parent;
        }
        return null;
    }
    
    public T? GetChild<T>() where T : Atom
    {
        foreach (var child in Children)
        {
            if (child is T t)
                return t;
        }
        return null;
    }
    
    private static Atom CreateDefaultInstance(Type type)
    {
        if (type is not { IsClass: true })
            throw new Exception("Type " + type.Name + " is not a valid component type (components must be classes).");
        
        if (type is not { IsAbstract: false })
            throw new Exception("Type " + type.Name + " is not a valid component type (components must not be abstract).");
        
        var constructor = type.GetConstructor(Type.EmptyTypes);
        if (constructor == null)
            throw new Exception("Type " + type.Name + " is not a valid component type (components must have a parameterless constructor).");
        
        var newInstance = Activator.CreateInstance(type);
        if (newInstance is not Atom atom)
            throw new Exception("Type " + type.Name + " is not a valid component type (components must inherit from Atom).");
        
        return atom;
    }
}
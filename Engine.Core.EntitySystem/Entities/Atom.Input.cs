using System.Reflection;
using Engine.Core.Common;
using Engine.Core.Input;
using Engine.Core.Input.Attributes;
using Silk.NET.Input;
using EntitySystem_Services_InputService = Engine.Core.EntitySystem.Services.InputService;
using InputService = Engine.Core.EntitySystem.Services.InputService;
using Services_InputService = Engine.Core.EntitySystem.Services.InputService;

namespace Engine.Core.EntitySystem.Entities;

public partial class Atom
{
    private void InitializeInput()
    {
        var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var guid = Guid.NewGuid();

        var inputService = GetService<EntitySystem_Services_InputService>();
        
        var onInputActionMethods = methods
            .Where(m => m.IsDefined(typeof(IOnInputAttribute), false))
            .ToList();
        var onInputActionHeldMethods = methods
            .Where(m => m.IsDefined(typeof(IOnInputHeldAttribute), false))
            .ToList();
        var onInputActionReleasedMethods = methods
            .Where(m => m.IsDefined(typeof(IOnInputReleasedAttribute), false))
            .ToList();

        for (var index = 0; index < onInputActionMethods.Count; index++)
        {
            var method = onInputActionMethods[index];
            var groupId = guid + "-" + index;
            
            var attrs = (IOnInputBaseAttribute[])method.GetCustomAttributes(typeof(IOnInputAttribute), inherit: false);
            
            foreach (var attr in attrs)
            {
                var entry = MakeInputActionHandler(method, attr, groupId);
                if (attr.HasInputAction)
                {
                    if (!inputService.OnInputEvent.ContainsKey(attr.InputActionId))
                        inputService.OnInputEvent[attr.InputActionId] = [];
                    inputService.OnInputEvent[attr.InputActionId].Add(entry);
                }
                
                var key = attr.ExplicitKey;
                if (key == null) continue;
                
                if (!inputService.OnKeyboardKeyEvent.ContainsKey((Key)key))
                    inputService.OnKeyboardKeyEvent[(Key)key] = [];
                inputService.OnKeyboardKeyEvent[(Key)key].Add(entry);
            }
        }

        for (var index = 0; index < onInputActionHeldMethods.Count; index++)
        {
            // Methods within a group are summed and invoked together.
            var groupId = guid + "-" + index;
            var method = onInputActionHeldMethods[index];
            
            var attrs = (IOnInputBaseAttribute[])method.GetCustomAttributes(typeof(IOnInputHeldAttribute), inherit: false);
            
            foreach (var attr in attrs)
            {
                var entry = MakeInputHeldActionHandler(method, attr, groupId);
                if (attr.HasInputAction)
                {
                    if (!inputService.OnInputHeldEvent.ContainsKey(attr.InputActionId))
                        inputService.OnInputHeldEvent[attr.InputActionId] = [];
                    inputService.OnInputHeldEvent[attr.InputActionId].Add(entry);
                }

                var key = attr.ExplicitKey;
                if (key == null) continue;
                
                if (!inputService.OnKeyboardKeyHeldEvent.ContainsKey((Key)key))
                    inputService.OnKeyboardKeyHeldEvent[(Key)key] = [];
                inputService.OnKeyboardKeyHeldEvent[(Key)key].Add(entry);
            }
        }

        for (var index = 0; index < onInputActionReleasedMethods.Count; index++)
        {
            var method = onInputActionReleasedMethods[index];
            var groupId = guid + "-" + index;
            
            var attrs = (IOnInputBaseAttribute[])method.GetCustomAttributes(typeof(IOnInputReleasedAttribute), inherit: false);
            
            foreach (var attr in attrs)
            {
                var entry = MakeInputActionHandler(method, attr, groupId);
                if (attr.HasInputAction)
                {
                    if (!inputService.OnInputReleasedEvent.ContainsKey(attr.InputActionId))
                        inputService.OnInputReleasedEvent[attr.InputActionId] = [];
                    inputService.OnInputReleasedEvent[attr.InputActionId].Add(entry);
                }
                
                var key = attr.ExplicitKey;
                if (key == null) continue;
                
                if (!inputService.OnKeyboardKeyReleasedEvent.ContainsKey((Key)key))
                    inputService.OnKeyboardKeyReleasedEvent[(Key)key] = [];
                inputService.OnKeyboardKeyReleasedEvent[(Key)key].Add(entry);
            }
        }
    }
    
    private BoundHeldAction MakeInputActionHandler(MethodInfo method, IOnInputBaseAttribute attr, string groupId)
    {
        var innerExpression = attr.BindingParams switch
        {
            InputParamBinding.None => 
                new BoundHeldAction(
                    this,
                    groupId,
                    0.0, 0.0, 0.0,
                    (_,_,_,_) =>
                        ((Action)Delegate.CreateDelegate(typeof(Action), this, method))()
                ),
            InputParamBinding.Double => 
                new BoundHeldAction(
                    this,
                    groupId,
                    attr.X, 0.0, 0.0,
                    (x,_,_,_) =>
                        ((Action<double>)Delegate.CreateDelegate(typeof(Action<double>), this, method))(x)
                ),
            InputParamBinding.Vector2 => 
                new BoundHeldAction(
                    this,
                    groupId,
                    attr.X, attr.Y, 0.0,
                    (x,y,_,_) =>
                        ((Action<Vector2>)Delegate.CreateDelegate(typeof(Action<Vector2>), this, method))(new Vector2(x, y))
                ),
            InputParamBinding.Vector3 =>
                new BoundHeldAction(
                    this,
                    groupId,
                    attr.X, attr.Y, attr.Z,
                    (x,y,z,_) =>
                        ((Action<Vector3>)Delegate.CreateDelegate(typeof(Action<Vector3>), this, method))(new Vector3(x, y, z))
                ),
            _ => throw new Exception("Unable to bind input action with binding params: " + attr.BindingParams)
        };
        return innerExpression;
    }

    private BoundHeldAction MakeInputHeldActionHandler(MethodInfo method, IOnInputBaseAttribute attr, string groupId)
    {
        var innerExpression = attr.BindingParams switch
        {
            InputParamBinding.None => GetInputHeldActionHandlerWithNoParams(method, groupId),
            InputParamBinding.Double => 
                new BoundHeldAction(
                    this,
                    groupId,
                    attr.X, 0.0, 0.0,
                    (delta,x,_,_) =>
                        ((Action<double, double>)Delegate.CreateDelegate(typeof(Action<double, double>), this, method))(delta,x)
                ),
            InputParamBinding.Vector2 => 
                new BoundHeldAction(
                    this,
                    groupId,
                    attr.X, attr.Y, 0.0,
                    (delta,x,y,_) =>
                        ((Action<double, Vector2>)Delegate.CreateDelegate(typeof(Action<double, Vector2>), this, method))(delta,new Vector2(x, y))
                ),
            InputParamBinding.Vector3 => 
                new BoundHeldAction(
                    this,
                    groupId,
                    attr.X, attr.Y, attr.Z,
                    (delta,x,y,z) =>
                        ((Action<double, Vector3>)Delegate.CreateDelegate(typeof(Action<double, Vector3>), this, method))(delta,new Vector3(x, y, z))
                ),
            _ => throw new Exception("Unable to bind input action with binding params: " + attr.BindingParams)
        };
        return innerExpression;
    }

    // Special case to differentiate between no params and only deltaTime
    private BoundHeldAction GetInputHeldActionHandlerWithNoParams(MethodInfo method, string groupId)
    {
        if (method.GetParameters().Length == 0)
        {
            return new BoundHeldAction(
                this,
                groupId,
                0.0, 0.0, 0.0,
                (_,_,_,_) =>
                    ((Action)Delegate.CreateDelegate(typeof(Action), this, method))()
            );
        }
        return new BoundHeldAction(
            this,
            groupId,
            0.0, 0.0, 0.0,
            (delta,_,_,_) =>
                ((Action<double>)Delegate.CreateDelegate(typeof(Action<double>), this, method))(delta)
        );
    }
}

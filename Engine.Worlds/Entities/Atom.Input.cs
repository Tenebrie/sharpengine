using System.Linq.Expressions;
using System.Reflection;
using Engine.Input;
using Engine.Input.Attributes;
using Engine.Worlds.Services;
using Silk.NET.Input;

namespace Engine.Worlds.Entities;

public partial class Atom
{
    private void InitializeInput()
    {
        var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var userInputActions = ReflectionService.GetUserInputActionsEnum();
        if (userInputActions == null)
        {
            throw new Exception("UserInputActionsEnum is null");
        }
        
        var guid = Guid.NewGuid();
        var onInputAttribute = typeof(OnInputAttribute<>).MakeGenericType(userInputActions);
        var onInputHeldAttribute = typeof(OnInputHeldAttribute<>).MakeGenericType(userInputActions);
        var onInputReleasedAttribute = typeof(OnInputReleasedAttribute<>).MakeGenericType(userInputActions);

        var onInputActionMethods = methods
            .Where(m => m.IsDefined(onInputAttribute, inherit: true))
            .ToList();
        var onInputActionHeldMethods = methods
            .Where(m => m.IsDefined(onInputHeldAttribute, inherit: true))
            .ToList();
        var onInputActionReleasedMethods = methods
            .Where(m => m.IsDefined(onInputReleasedAttribute, inherit: true))
            .ToList();
        
        foreach (var method in onInputActionMethods)
        {
            var attrs = (IOnInputAttribute[])method.GetCustomAttributes(onInputAttribute, inherit: false);
            foreach (var attr in attrs)
            {
                var entry = MakeInputActionHandler(method, attr);
                if (attr.HasInputAction)
                {
                    if (InputHandler.OnInputEvent.TryGetValue(attr.InputActionId, out _))
                        InputHandler.OnInputEvent[attr.InputActionId] += entry;
                    else
                        InputHandler.OnInputEvent[attr.InputActionId] = entry;
                }

                var key = attr.ExplicitKey;
                if (key == null) continue;
                if (InputHandler.OnKeyboardKeyEvent.TryGetValue((Key)key, out _))
                    InputHandler.OnKeyboardKeyEvent[(Key)key] += entry;
                else
                    InputHandler.OnKeyboardKeyEvent[(Key)key] = entry;
            }
        }

        for (var index = 0; index < onInputActionHeldMethods.Count; index++)
        {
            // Methods within a group are summed and invoked together.
            var groupId = guid + "-" + index;
            var method = onInputActionHeldMethods[index];
            var attrs = (IOnInputAttribute[])method.GetCustomAttributes(onInputHeldAttribute, inherit: false);
            foreach (var attr in attrs)
            {
                var entry = MakeInputHeldActionHandler(method, attr, groupId);
                if (attr.HasInputAction)
                {
                    if (!InputHandler.OnInputHeldEvent.ContainsKey(attr.InputActionId))
                        InputHandler.OnInputHeldEvent[attr.InputActionId] = [];
                    InputHandler.OnInputHeldEvent[attr.InputActionId].Add(entry);
                }

                var key = attr.ExplicitKey;
                if (key == null) continue;
                
                if (!InputHandler.OnKeyboardKeyHeldEvent.ContainsKey((Key)key))
                    InputHandler.OnKeyboardKeyHeldEvent[(Key)key] = [];
                InputHandler.OnKeyboardKeyHeldEvent[(Key)key].Add(entry);
            }
        }

        foreach (var method in onInputActionReleasedMethods)
        {
            var attrs = (IOnInputAttribute[])method.GetCustomAttributes(onInputReleasedAttribute, inherit: false);
            foreach (var attr in attrs)
            {
                var entry = MakeInputActionHandler(method, attr);
                if (attr.HasInputAction)
                {
                    if (InputHandler.OnInputReleasedEvent.TryGetValue(attr.InputActionId, out _))
                        InputHandler.OnInputReleasedEvent[attr.InputActionId] += entry;
                    else
                        InputHandler.OnInputReleasedEvent[attr.InputActionId] = entry;
                }

                var key = attr.ExplicitKey;
                if (key == null) continue;
                if (InputHandler.OnKeyboardKeyReleasedEvent.TryGetValue((Key)key, out _))
                    InputHandler.OnKeyboardKeyReleasedEvent[(Key)key] += entry;
                else
                    InputHandler.OnKeyboardKeyReleasedEvent[(Key)key] = entry;
            }
        }
    }
    
    private Action MakeInputActionHandler(MethodInfo method, IOnInputAttribute attr)
    {
        LambdaExpression innerExpression = attr.BindingParams switch
        {
            InputParamBinding.None => () =>
                ((Action)Delegate.CreateDelegate(typeof(Action), this, method))(),
            InputParamBinding.Double => () =>
                ((Action<double>)Delegate.CreateDelegate(typeof(Action<double>), this, method))(attr.X),
            InputParamBinding.Vector2 => () =>
                ((Action<Vector2>)Delegate.CreateDelegate(typeof(Action<Vector2>), this, method))(new Vector2(attr.X, attr.Y)),
            InputParamBinding.Vector3 => () =>
                ((Action<Vector>)Delegate.CreateDelegate(typeof(Action<Vector>), this, method))(new Vector(attr.X, attr.Y, attr.Z)),
            _ => throw new Exception("Unable to bind input action with binding params: " + attr.BindingParams)
        };
        return (Action)innerExpression.Compile();
    }

    private BoundHeldAction MakeInputHeldActionHandler(MethodInfo method, IOnInputAttribute attr, string groupId)
    {
        BoundHeldAction innerExpression = attr.BindingParams switch
        {
            InputParamBinding.None => 
                new BoundHeldAction(
                    this,
                    groupId,
                    0.0, 0.0, 0.0,
                    (delta,_,_,_) =>
                        ((Action<double>)Delegate.CreateDelegate(typeof(Action<double>), this, method))(delta)
                ),
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
                    attr.X, attr.Y, 0.0,
                    (delta,x,y,z) =>
                        ((Action<double, Vector>)Delegate.CreateDelegate(typeof(Action<double, Vector>), this, method))(delta,new Vector(x,y,z))
                ),
            _ => throw new Exception("Unable to bind input action with binding params: " + attr.BindingParams)
        };
        return innerExpression;
    }
}

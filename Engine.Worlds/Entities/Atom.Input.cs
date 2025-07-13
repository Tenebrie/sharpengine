using System.Reflection;
using Engine.Input;
using Engine.Input.Attributes;
using Engine.Worlds.Services;
using Silk.NET.Input;
using InputService = Engine.Input.InputService;

namespace Engine.Worlds.Entities;

public partial class Atom
{
    private void InitializeInput()
    {
        var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var userInputActions = Backstage.ServiceRegistry.Get<ReflectionService>().GetUserInputActionsEnum();
        
        var guid = Guid.NewGuid();
        List<MethodInfo> onInputActionMethods;
        List<MethodInfo> onInputActionHeldMethods;
        List<MethodInfo> onInputActionReleasedMethods;
        Type? onInputAttribute = null;
        Type? onInputHeldAttribute = null;
        Type? onInputReleasedAttribute = null;

        // No actions defined - process only hardcoded key events.
        if (userInputActions == null)
        {
            onInputActionMethods = methods
                .Where(m => m.IsDefined(typeof(OnKeyInputAttribute), inherit: true))
                .ToList();
            onInputActionHeldMethods = methods
                .Where(m => m.IsDefined(typeof(OnKeyInputHeldAttribute), inherit: true))
                .ToList();
            onInputActionReleasedMethods = methods
                .Where(m => m.IsDefined(typeof(OnKeyInputReleasedAttribute), inherit: true))
                .ToList();
        }
        // Actions defined - process all.
        else
        {
            onInputAttribute = typeof(OnInputAttribute<>).MakeGenericType(userInputActions);
            onInputHeldAttribute = typeof(OnInputHeldAttribute<>).MakeGenericType(userInputActions);
            onInputReleasedAttribute = typeof(OnInputReleasedAttribute<>).MakeGenericType(userInputActions);

            onInputActionMethods = methods
                .Where(m => m.IsDefined(typeof(OnKeyInputAttribute)) || m.IsDefined(onInputAttribute, inherit: true))
                .ToList();
            onInputActionHeldMethods = methods
                .Where(m => m.IsDefined(typeof(OnKeyInputHeldAttribute)) || m.IsDefined(onInputHeldAttribute, inherit: true))
                .ToList();
            onInputActionReleasedMethods = methods
                .Where(m => m.IsDefined(typeof(OnKeyInputReleasedAttribute)) || m.IsDefined(onInputReleasedAttribute, inherit: true))
                .ToList();
        }

        for (var index = 0; index < onInputActionMethods.Count; index++)
        {
            var method = onInputActionMethods[index];
            var groupId = guid + "-" + index;
            
            var attrs = (IOnInputAttribute[])method.GetCustomAttributes(typeof(OnKeyInputAttribute), inherit: false);
            if (onInputAttribute != null)
                attrs = attrs.Concat((IOnInputAttribute[])method.GetCustomAttributes(onInputAttribute, inherit: false)).ToArray();
            
            foreach (var attr in attrs)
            {
                var entry = MakeInputActionHandler(method, attr, groupId);
                if (attr.HasInputAction)
                {
                    if (!InputService.OnInputEvent.ContainsKey(attr.InputActionId))
                        InputService.OnInputEvent[attr.InputActionId] = [];
                    InputService.OnInputEvent[attr.InputActionId].Add(entry);
                }
                
                var key = attr.ExplicitKey;
                if (key == null) continue;
                
                if (!InputService.OnKeyboardKeyEvent.ContainsKey((Key)key))
                    InputService.OnKeyboardKeyEvent[(Key)key] = [];
                InputService.OnKeyboardKeyEvent[(Key)key].Add(entry);
            }
        }

        for (var index = 0; index < onInputActionHeldMethods.Count; index++)
        {
            // Methods within a group are summed and invoked together.
            var groupId = guid + "-" + index;
            var method = onInputActionHeldMethods[index];
            
            var attrs = (IOnInputAttribute[])method.GetCustomAttributes(typeof(OnKeyInputHeldAttribute), inherit: false);
            if (onInputHeldAttribute != null)
                attrs = attrs.Concat((IOnInputAttribute[])method.GetCustomAttributes(onInputHeldAttribute, inherit: false)).ToArray();
            
            foreach (var attr in attrs)
            {
                var entry = MakeInputHeldActionHandler(method, attr, groupId);
                if (attr.HasInputAction)
                {
                    if (!InputService.OnInputHeldEvent.ContainsKey(attr.InputActionId))
                        InputService.OnInputHeldEvent[attr.InputActionId] = [];
                    InputService.OnInputHeldEvent[attr.InputActionId].Add(entry);
                }

                var key = attr.ExplicitKey;
                if (key == null) continue;
                
                if (!InputService.OnKeyboardKeyHeldEvent.ContainsKey((Key)key))
                    InputService.OnKeyboardKeyHeldEvent[(Key)key] = [];
                InputService.OnKeyboardKeyHeldEvent[(Key)key].Add(entry);
            }
        }

        for (var index = 0; index < onInputActionReleasedMethods.Count; index++)
        {
            var method = onInputActionReleasedMethods[index];
            var groupId = guid + "-" + index;
            
            var attrs = (IOnInputAttribute[])method.GetCustomAttributes(typeof(OnKeyInputReleasedAttribute), inherit: false);
            if (onInputReleasedAttribute != null)
                attrs = attrs.Concat((IOnInputAttribute[])method.GetCustomAttributes(onInputReleasedAttribute, inherit: false)).ToArray();
            
            foreach (var attr in attrs)
            {
                var entry = MakeInputActionHandler(method, attr, groupId);
                if (attr.HasInputAction)
                {
                    if (!InputService.OnInputReleasedEvent.ContainsKey(attr.InputActionId))
                        InputService.OnInputReleasedEvent[attr.InputActionId] = [];
                    InputService.OnInputReleasedEvent[attr.InputActionId].Add(entry);
                }
                
                var key = attr.ExplicitKey;
                if (key == null) continue;
                
                if (!InputService.OnKeyboardKeyReleasedEvent.ContainsKey((Key)key))
                    InputService.OnKeyboardKeyReleasedEvent[(Key)key] = [];
                InputService.OnKeyboardKeyReleasedEvent[(Key)key].Add(entry);
            }
        }
    }
    
    private BoundHeldAction MakeInputActionHandler(MethodInfo method, IOnInputAttribute attr, string groupId)
    {
        var innerExpression = attr.BindingParams switch
        {
            InputParamBinding.None => 
                new BoundHeldAction(
                    this,
                    groupId,
                    0.0, 0.0,
                    (_,_,_) =>
                        ((Action)Delegate.CreateDelegate(typeof(Action), this, method))()
                ),
            InputParamBinding.Double => 
                new BoundHeldAction(
                    this,
                    groupId,
                    attr.X, 0.0,
                    (x,_,_) =>
                        ((Action<double>)Delegate.CreateDelegate(typeof(Action<double>), this, method))(x)
                ),
            InputParamBinding.Vector2 => 
                new BoundHeldAction(
                    this,
                    groupId,
                    attr.X, attr.Y,
                    (x,y,_) =>
                        ((Action<Vector2>)Delegate.CreateDelegate(typeof(Action<Vector2>), this, method))(new Vector2(x, y))
                ),
            _ => throw new Exception("Unable to bind input action with binding params: " + attr.BindingParams)
        };
        return innerExpression;
    }

    private BoundHeldAction MakeInputHeldActionHandler(MethodInfo method, IOnInputAttribute attr, string groupId)
    {
        var innerExpression = attr.BindingParams switch
        {
            InputParamBinding.None => GetInputHeldActionHandlerWithNoParams(method, attr, groupId),
            InputParamBinding.Double => 
                new BoundHeldAction(
                    this,
                    groupId,
                    attr.X, 0.0,
                    (delta,x,_) =>
                        ((Action<double, double>)Delegate.CreateDelegate(typeof(Action<double, double>), this, method))(delta,x)
                ),
            InputParamBinding.Vector2 => 
                new BoundHeldAction(
                    this,
                    groupId,
                    attr.X, attr.Y,
                    (delta,x,y) =>
                        ((Action<double, Vector2>)Delegate.CreateDelegate(typeof(Action<double, Vector2>), this, method))(delta,new Vector2(x, y))
                ),
            _ => throw new Exception("Unable to bind input action with binding params: " + attr.BindingParams)
        };
        return innerExpression;
    }

    // Special case to differentiate between no params and only deltaTime
    private BoundHeldAction GetInputHeldActionHandlerWithNoParams(MethodInfo method, IOnInputAttribute attr,
        string groupId)
    {
        if (method.GetParameters().Length == 0)
        {
            return new BoundHeldAction(
                this,
                groupId,
                0.0, 0.0,
                (_,_,_) =>
                    ((Action)Delegate.CreateDelegate(typeof(Action), this, method))()
            );
        }
        return new BoundHeldAction(
            this,
            groupId,
            0.0, 0.0,
            (delta,_,_) =>
                ((Action<double>)Delegate.CreateDelegate(typeof(Action<double>), this, method))(delta)
        );
    }
}

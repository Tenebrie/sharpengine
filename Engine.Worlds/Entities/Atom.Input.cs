using System.Reflection;
using Engine.Input;
using Engine.Input.Attributes;

namespace Engine.Worlds.Entities;

public partial class Atom
{
    private void InitializeInput()
    {
        var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        
        var onInputActionMethods = methods.Where(method => method.GetCustomAttributes<OnInputActionAttribute>().Any()).ToList();
        var onInputActionHeldMethods = methods.Where(method => method.GetCustomAttributes<OnInputActionHeldAttribute>().Any()).ToList();
        var onInputActionReleasedMethods = methods.Where(method => method.GetCustomAttributes<OnInputActionReleasedAttribute>().Any()).ToList();
        
        foreach (var method in onInputActionMethods)
        {
            var attrs = method.GetCustomAttributes<OnInputActionAttribute>(inherit: false);

            foreach (var attr in attrs)
            {
                var entry = new KeyboardEventAction(
                    (Action<double>)Delegate.CreateDelegate(typeof(Action<double>), this, method), attr.Value);
                if (InputHandler.OnKeyboardEvent.TryGetValue(attr.Key, out var existingList))
                    existingList.Add(entry);
                else
                    InputHandler.OnKeyboardEvent[attr.Key] = [entry];
            }
        }
        foreach (var method in onInputActionHeldMethods)
        {
            var attrs = method.GetCustomAttributes<OnInputActionHeldAttribute>(inherit: false);

            foreach (var attr in attrs)
            {
                var entry = new KeyboardHeldEventAction(
                    (Action<double, double>)Delegate.CreateDelegate(typeof(Action<double, double>), this, method), attr.Value);
                if (InputHandler.OnKeyboardHeldEvent.TryGetValue(attr.Key, out var existingList))
                    existingList.Add(entry);
                else
                    InputHandler.OnKeyboardHeldEvent[attr.Key] = [entry];
            }
        }
        foreach (var method in onInputActionReleasedMethods)
        {
            var attrs = method.GetCustomAttributes<OnInputActionReleasedAttribute>(inherit: false);

            foreach (var attr in attrs)
            {
                var entry = new KeyboardEventAction(
                    (Action<double>)Delegate.CreateDelegate(typeof(Action<double>), this, method), attr.Value);
                if (InputHandler.OnKeyboardReleasedEvent.TryGetValue(attr.Key, out var existingList))
                    existingList.Add(entry);
                else
                    InputHandler.OnKeyboardReleasedEvent[attr.Key] = [entry];
            }
        }
    }
}
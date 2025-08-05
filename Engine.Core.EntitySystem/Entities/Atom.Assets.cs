using System.Reflection;
using Engine.Core.Assets;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.Logging;

namespace Engine.Core.EntitySystem.Entities;

public partial class Atom
{
    [OnCreate]
    internal static void OnStaticPrepare(Type type)
    {
        Console.WriteLine("Preparing resources for " + type.FullName);
        var prepareMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(methodInfo => methodInfo.GetCustomAttribute<OnPrepareResourcesAttribute>() != null);
        var index = 0;
        foreach (var prepareResourcesMethod in prepareMethods)
        {
            var indexKey = index;
            // Increment index even on early out
            index += 1;
            
            if (AssetManager.Prepared.Has(type, indexKey))
                continue;
            
            var userData = prepareResourcesMethod.Invoke(null, null);
            Console.WriteLine("USER DATA IS " + userData);
            if (userData is null)
                continue;
            AssetManager.Prepared.Save(type, indexKey, userData);
        }
        
        var staticLoadMethods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(methodInfo => methodInfo.GetCustomAttribute<OnLoadResourcesAttribute>() != null);
        index = 0;
        foreach (var staticLoadMethod in staticLoadMethods)
        {
            var indexKey = index;
            // Increment index even on early out
            index += 1;

            if (!AssetManager.Prepared.Has(type, indexKey))
                throw new InvalidOperationException("No resource prepared for " + type.FullName + " at index " + indexKey);
            var userData = AssetManager.Prepared.Load<object?>(type, indexKey);
            if (userData is null)
                continue;
            staticLoadMethod.Invoke(null, [userData]);
        }
    }
}

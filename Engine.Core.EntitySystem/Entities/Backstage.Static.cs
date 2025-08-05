using System.Reflection;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.Logging;

namespace Engine.Core.EntitySystem.Entities;

public partial class Backstage : Scene
{
    private void RunAssemblyStaticInit()
    {
        foreach (var atomType in GetAssemblyAtomTypes())
        {
            var initMethods = atomType
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                .Where(methodInfo => methodInfo.GetCustomAttribute<OnCreateAttribute>() != null).ToList();
            foreach (var methodInfo in initMethods)
            {
                if (methodInfo.GetParameters().Length == 0)
                {
                    var methodDelegate = Delegate.CreateDelegate(typeof(Action), null, methodInfo);
                    ((Action)methodDelegate).Invoke();
                }
                else if (methodInfo.GetParameters().Length == 1)
                {
                    var methodDelegate = Delegate.CreateDelegate(typeof(Action<Type>), null, methodInfo);
                    ((Action<Type>)methodDelegate).Invoke(atomType);
                }
                else
                    throw new InvalidOperationException(
                        $"Method {methodInfo.Name} in type {atomType.FullName} has an invalid signature. "
                    );
            };
        }
    }
}

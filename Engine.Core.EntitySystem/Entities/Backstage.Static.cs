using System.Reflection;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.Logging;

namespace Engine.Core.EntitySystem.Entities;

public partial class Backstage
{
    private void RunAssemblyStaticInit()
    {
        foreach (var atomType in GetAssemblyAtomTypes())
        {
            var initMethods = atomType
                .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                .Where(methodInfo =>
                    methodInfo.GetCustomAttribute<OnCreateAttribute>() != null ||
                    methodInfo.GetCustomAttribute<OnPrepareResourcesAttribute>() != null)
                .ToList();
            foreach (var methodInfo in initMethods)
            {
                if (methodInfo.GetParameters().Length == 0)
                {
                    var methodDelegate = Delegate.CreateDelegate(typeof(Action), null, methodInfo);
                    ((Action)methodDelegate).Invoke();
                }
                else if (methodInfo.GetParameters().Length == 1)
                {
                    var methodDelegate = Delegate.CreateDelegate(typeof(Action<Backstage>), null, methodInfo);
                    ((Action<Backstage>)methodDelegate).Invoke(this);
                }
                else if (methodInfo.GetParameters().Length == 2)
                {
                    var methodDelegate = Delegate.CreateDelegate(typeof(Action<Backstage, Type>), null, methodInfo);
                    ((Action<Backstage, Type>)methodDelegate).Invoke(this, atomType);
                }
                else
                    throw new InvalidOperationException(
                        $"Method {methodInfo.Name} in type {atomType.FullName} has an invalid signature. "
                    );
            }
            ;
        }
    }
}

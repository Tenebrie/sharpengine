using System.Linq.Expressions;
using System.Reflection;
using Engine.Core.Attributes;
using Engine.Core.Communication.Groups;
using Engine.Core.Communication.Signals;
using Engine.Core.EntitySystem.Attributes;
using Engine.Core.Input.Attributes;

namespace Engine.Core.EntitySystem.Entities;

public partial class Atom
{
    private static readonly Dictionary<Type, ReflectionData> ReflectionDataCache = new();
    
    private void InitializeReflection()
    {
        if (ReflectionDataCache.ContainsKey(GetType()))
            return;
        
        var allFields = GetType().GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        var instanceFields = allFields.Where(fieldInfo => !fieldInfo.IsStatic).ToList();
        var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        var reflectionData = new ReflectionData
        {
            OnCreateMethods = methods.Select(ReflectionMethod<OnCreateAttribute>.Create).OfType<ReflectionMethod<OnCreateAttribute>>().ToList(),
            OnReadyMethods = methods.Select(ReflectionMethod<OnReadyAttribute>.Create).OfType<ReflectionMethod<OnReadyAttribute>>().ToList(),
            OnUpdateMethods = methods.Select(ReflectionMethod<OnUpdateAttribute>.Create).OfType<ReflectionMethod<OnUpdateAttribute>>().ToList(),
            OnDestroyMethods = methods.Select(ReflectionMethod<OnDestroyAttribute>.Create).OfType<ReflectionMethod<OnDestroyAttribute>>().ToList(),
            OnModuleReloadMethods = methods.Select(ReflectionMethod<OnModuleReloadAttribute>.Create).OfType<ReflectionMethod<OnModuleReloadAttribute>>().ToList(),
            OnGameplayContextChangeMethods = methods.Select(ReflectionMethod<OnGameplayContextChangeAttribute>.Create).OfType<ReflectionMethod<OnGameplayContextChangeAttribute>>().ToList(),
            OnInputMethods = methods.Select(ReflectionInputMethod<IOnInputAttribute>.Create).OfType<ReflectionInputMethod<IOnInputAttribute>>().ToList(),
            OnInputHeldMethods = methods.Select(ReflectionInputMethod<IOnInputHeldAttribute>.Create).OfType<ReflectionInputMethod<IOnInputHeldAttribute>>().ToList(),
            OnInputReleasedMethods = methods.Select(ReflectionInputMethod<IOnInputReleasedAttribute>.Create).OfType<ReflectionInputMethod<IOnInputReleasedAttribute>>().ToList(),
            OnTimerMethods = methods.Select(ReflectionMethod<OnTimerAttribute>.Create).OfType<ReflectionMethod<OnTimerAttribute>>().ToList(),
            
            SignalFields = allFields.Select(ReflectionField<ISignal, SignalAttribute>.Create).OfType<ReflectionField<ISignal, SignalAttribute>>().ToList(),
            ComponentFields = instanceFields.Select(ReflectionField<Atom, ComponentAttribute>.Create).OfType<ReflectionField<Atom, ComponentAttribute>>().ToList(),
            DefaultGroupFields = allFields.Select(ReflectionField<IGroup, DefaultGroupAttribute>.Create).OfType<ReflectionField<IGroup, DefaultGroupAttribute>>().ToList()
        };

        ReflectionDataCache[GetType()] = reflectionData;
    }
    
    internal readonly struct ReflectionData
    {
        public required List<ReflectionMethod<OnCreateAttribute>> OnCreateMethods { get; init; }
        public required List<ReflectionMethod<OnReadyAttribute>> OnReadyMethods { get; init; }
        public required List<ReflectionMethod<OnUpdateAttribute>> OnUpdateMethods { get; init; }
        public required List<ReflectionMethod<OnDestroyAttribute>> OnDestroyMethods { get; init; }
        public required List<ReflectionMethod<OnModuleReloadAttribute>> OnModuleReloadMethods { get; init; }
        public required List<ReflectionMethod<OnGameplayContextChangeAttribute>> OnGameplayContextChangeMethods { get; init; }
        public required List<ReflectionInputMethod<IOnInputAttribute>> OnInputMethods { get; init; }
        public required List<ReflectionInputMethod<IOnInputHeldAttribute>> OnInputHeldMethods { get; init; }
        public required List<ReflectionInputMethod<IOnInputReleasedAttribute>> OnInputReleasedMethods { get; init; }
        public required List<ReflectionMethod<OnTimerAttribute>> OnTimerMethods { get; init; }
        
        public required List<ReflectionField<ISignal, SignalAttribute>> SignalFields { get; init; }
        public required List<ReflectionField<Atom, ComponentAttribute>> ComponentFields { get; init; }
        public required List<ReflectionField<IGroup, DefaultGroupAttribute>> DefaultGroupFields { get; init; }
    }

    internal readonly struct ReflectionMethod<T> where T : Attribute
    {
        public required MethodInfo MethodInfo { get; init; }
        public required T Attribute { get; init; }
        public required int ParameterCount { get; init; }

        public static ReflectionMethod<T>? Create(MethodInfo methodInfo)
        {
            if (methodInfo.IsStatic)
                return null;
            var attribute = methodInfo.GetCustomAttribute<T>();
            if (attribute == null)
                return null;
            return new ReflectionMethod<T>
            {
                MethodInfo = methodInfo,
                Attribute = attribute,
                ParameterCount = methodInfo.GetParameters().Length
            };
        }
    }
    
    internal readonly struct ReflectionInputMethod<T> where T : IOnInputBaseAttribute
    {
        public required MethodInfo MethodInfo { get; init; }
        public required IOnInputBaseAttribute[] Attributes { get; init; }

        public static ReflectionInputMethod<T>? Create(MethodInfo methodInfo)
        {
            if (methodInfo.IsStatic)
                return null;
            var attributes = (IOnInputBaseAttribute[])methodInfo.GetCustomAttributes(typeof(T), inherit: false);
            if (attributes.Length == 0)
                return null;
            return new ReflectionInputMethod<T>
            {
                MethodInfo = methodInfo,
                Attributes = attributes
            };
        }
    }

    internal readonly struct ReflectionField<TObject, T> where T : Attribute
    {
        public required FieldInfo FieldInfo { get; init; }
        public required T Attribute { get; init; }
        public required bool IsStatic { get; init; }
        public required Func<Atom?, TObject?> GetValue { get; init; }
        public required Action<Atom, TObject> SetValue { get; init; }
        public required Func<TObject> Factory { get; init; }
        
        public static ReflectionField<TObject, T>? Create(FieldInfo fieldInfo)
        {
            var attribute = fieldInfo.GetCustomAttribute<T>();
            if (attribute == null)
                return null;

            return new ReflectionField<TObject, T>
            {
                FieldInfo = fieldInfo,
                Attribute = attribute,
                IsStatic = fieldInfo.IsStatic,
                GetValue = MakeGetter(fieldInfo),
                SetValue = MakeSetter(fieldInfo),
                Factory = BuildFactory(fieldInfo)
            };
        }
        
        private static Func<Atom?, TObject?> MakeGetter(FieldInfo fieldInfo)
        {
            if (fieldInfo.IsStatic)
            {
                var value = Expression.Field(null, fieldInfo);
                return Expression.Lambda<Func<Atom?, TObject?>>(Expression.Convert(value, typeof(TObject)), Expression.Parameter(typeof(Atom), "target")).Compile();
            }

            var target = Expression.Parameter(typeof(Atom), "target");
            var typedTarget = Expression.Convert(target, fieldInfo.DeclaringType!);
            var fieldExpr = Expression.Field(typedTarget, fieldInfo);

            return Expression.Lambda<Func<Atom?, TObject?>>(Expression.Convert(fieldExpr, typeof(TObject)), target).Compile();
        }
        
        private static Action<Atom, TObject> MakeSetter(FieldInfo fieldInfo)
        {
            if (fieldInfo.IsStatic)
                return (_, _) => { };

            var target = Expression.Parameter(typeof(Atom), "target");
            var value  = Expression.Parameter(typeof(TObject), "value");

            var typedTarget = Expression.Convert(target, fieldInfo.DeclaringType!);
            var typedValue  = Expression.Convert(value, fieldInfo.FieldType);
            var fieldExpr   = Expression.Field(typedTarget, fieldInfo);

            var assign = Expression.Assign(fieldExpr, typedValue);
            return Expression.Lambda<Action<Atom, TObject>>(assign, target, value).Compile();
        }
        
        private static Func<TObject> BuildFactory(FieldInfo fieldInfo)
        {
            var type = fieldInfo.FieldType;
            if (type is not { IsClass: true })
                throw new Exception("Type " + type.Name + " is not a valid component type (components must be classes).");
        
            if (type is not { IsAbstract: false })
                throw new Exception("Type " + type.Name + " is not a valid component type (components must not be abstract).");

            if (type.IsValueType)
            {
                var valueBody = Expression.Convert(Expression.Default(type), typeof(TObject));
                return Expression.Lambda<Func<TObject>>(valueBody).Compile();
            }
            
            var constructor = type.GetConstructor(Type.EmptyTypes);
            if (constructor == null)
                throw new Exception("Type " + type.Name + " is not a valid component type (components must have a parameterless constructor).");

            var newExpr = Expression.New(constructor);
            var classBody = Expression.Convert(newExpr, typeof(TObject));
            return Expression.Lambda<Func<TObject>>(classBody).Compile();
        }
    }
}
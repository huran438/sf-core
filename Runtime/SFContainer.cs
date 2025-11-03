using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace SFramework.Core.Runtime
{
    public static class SFContainer
    {
        private static readonly Dictionary<Type, SFInjectableTypeInfo> _injectableTypes;
        private static readonly Dictionary<Type, object> _dependencies = new();
        private static readonly Dictionary<Type, List<Type>> _mapping = new();
        private static readonly List<ISFService> _services = new();

        static SFContainer()
        {
            _injectableTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => type.IsClass && typeof(ISFInjectable).IsAssignableFrom(type))
                .Select(type => new SFInjectableTypeInfo(ref type))
                .ToDictionary(typeInfo => typeInfo.Type, t => t);
            
            _dependencies.Clear();
            _mapping.Clear();
            _services.Clear();
        }

        public static TService Register<TService, TImplementation>() where TImplementation : class, TService
        {
            if (_dependencies.ContainsKey(typeof(TService)))
            {
                if (SFDebug.IsDebug)
                {
                    SFDebug.Log("Object of this type already exists in the dependency container", LogType.Warning);
                }
            }

            object instance;

            var constructor = typeof(TImplementation).GetTypeInfo().DeclaredConstructors.FirstOrDefault();

            if (constructor != null)
            {
                var parameters = constructor.GetParameters();

                if (parameters.Length > 0)
                {
                    var parameterInstances = new object[parameters.Length];
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (!_dependencies.TryGetValue(parameters[i].ParameterType, out var obj))
                        {
                            if (SFDebug.IsDebug)
                            {
                                SFDebug.Log($"Service of type {parameters[i].ParameterType.FullName} is not registered. Returning NULL.", LogType.Error);
                            }

                            break;
                        }

                        parameterInstances[i] = obj;
                    }

                    instance = constructor.Invoke(parameterInstances);
                }
                else
                {
                    instance = Activator.CreateInstance(typeof(TImplementation), true);
                }
            }
            else
            {
                instance = Activator.CreateInstance(typeof(TImplementation), true);
            }

            return Register<TService, TImplementation>(instance);
        }

        public static TService Register<TService, TImplementation>(object instance) where TImplementation : class, TService
        {
            if (_dependencies.ContainsKey(typeof(TService)))
            {
                if (SFDebug.IsDebug)
                {
                    SFDebug.Log("Object of this type already exists in the dependency container", LogType.Warning);
                }
            }

            if (SFDebug.IsDebug)
            {
                SFDebug.Log($"[Core] Bind: {typeof(TService).Name} to {typeof(TImplementation).Name}");
            }

            foreach (var subclassType in typeof(TService).GetInterfaces())
            {
                if (!_mapping.ContainsKey(subclassType))
                {
                    _mapping[subclassType] = new List<Type>();
                }

                _mapping[subclassType].Add(typeof(TService));
            }

            _dependencies[typeof(TService)] = instance ?? throw new ArgumentNullException(nameof(instance));

            if (typeof(ISFService).IsAssignableFrom(typeof(TService)))
            {
                _services.Add(instance as ISFService);
            }

            return (TService)instance;
        }

        public static TService Register<TService>(object instance)
        {
            if (instance is not TService) return default;

            if (_dependencies.ContainsKey(typeof(TService)))
            {
                if (SFDebug.IsDebug)
                {
                    SFDebug.Log("Object of this type already exists in the dependency container", LogType.Warning);
                }
            }

            if (SFDebug.IsDebug)
            {
                SFDebug.Log($"[Core] Bind: {typeof(TService).Name} to {instance.GetType().Name}");
            }

            foreach (var subclassType in typeof(TService).GetInterfaces())
            {
                if (!_mapping.ContainsKey(subclassType))
                {
                    _mapping[subclassType] = new List<Type>();
                }

                _mapping[subclassType].Add(typeof(TService));
            }

            _dependencies[typeof(TService)] = instance ?? throw new ArgumentNullException(nameof(instance));

            if (typeof(ISFService).IsAssignableFrom(typeof(TService)))
            {
                _services.Add(instance as ISFService);
            }

            return (TService)instance;
        }

        public static void Register(Type type, object instance)
        {
            if (_dependencies.ContainsKey(type))
            {
                if (SFDebug.IsDebug)
                {
                    SFDebug.Log("Object of this type already exists in the dependency container", LogType.Warning);
                }
            }


            if (SFDebug.IsDebug)
            {
                SFDebug.Log($"[Core] Bind: {type.Name} to {instance.GetType().Name}");
            }

            foreach (var subclassType in type.GetInterfaces())
            {
                if (!_mapping.ContainsKey(subclassType))
                {
                    _mapping[subclassType] = new List<Type>();
                }

                _mapping[subclassType].Add(type);
            }

            _dependencies[type] = instance ?? throw new ArgumentNullException(nameof(instance));

            if (typeof(ISFService).IsAssignableFrom(type))
            {
                _services.Add(instance as ISFService);
            }
        }
        
        public static T Resolve<T>() where T : class
        {
            return Resolve(typeof(T)) as T;
        }

        public static T[] ResolveMany<T>()
        {
            if (!_mapping.ContainsKey(typeof(T))) return Array.Empty<T>();

            var result = new T[_mapping[typeof(T)].Count];

            for (int i = 0; i < _mapping[typeof(T)].Count; i++)
            {
                var type = _mapping[typeof(T)][i];
                result[i] = (T)Resolve(type);
            }

            return result;
        }

        public static object Resolve(Type type)
        {
            if (_dependencies.TryGetValue(type, out var resolve))
            {
                return resolve;
            }

            return _dependencies.FirstOrDefault(kvp => type.IsAssignableFrom(kvp.Key)).Value;
        }

        public static object[] Bindings => _dependencies.Values.ToArray();

        public static async UniTask InitServices(CancellationToken cancellationToken)
        {
            foreach (var service in _services)
            {
                await service.Init(cancellationToken);
            }
        }


        public static IEnumerable<T> GetDependencies<T>() where T : class
        {
            var result = new List<T>();

            foreach (var dependency in _dependencies)
            {
                var typedDependency = dependency.Value as T;
                if (typedDependency != null)
                {
                    result.Add(typedDependency);
                }
            }

            return result;
        }
        
        public static void Setup()
        {
            foreach (var dependency in _dependencies.Values)
            {
                Inject(dependency);
            }
        }

        public static void Inject(GameObject gameObject, bool includeInactive = false)
        {
            foreach (var injectable in gameObject.GetComponentsInChildren<ISFInjectable>(includeInactive))
            {
                Inject(injectable);
            }
        }

        public static void Inject(object targetObject)
        {
            if (targetObject == null) throw new ArgumentNullException(nameof(targetObject));

            if (!_injectableTypes.TryGetValue(targetObject.GetType(), out var injectableType)) return;

            InjectFields(ref targetObject, ref injectableType.Fields);
            InjectProperties(ref targetObject, ref injectableType.Properties);
            InjectMethods(ref targetObject, ref injectableType.Methods, ref injectableType.ParametersByMethod);
        }


        private static void InjectFields(ref object targetObject, ref FieldInfo[] injectableFields)
        {
            foreach (var fieldInfo in injectableFields)
            {
                var dependency = Resolve(fieldInfo.FieldType);
                fieldInfo.SetValue(targetObject, dependency);
            }
        }

        private static void InjectProperties(ref object targetObject, ref PropertyInfo[] injectableProperties)
        {
            foreach (var propertyInfo in injectableProperties)
            {
                var dependency = Resolve(propertyInfo.PropertyType);
                propertyInfo.SetValue(targetObject, dependency);
            }
        }

        private static void InjectMethods(ref object targetObject, ref MethodInfo[] methods,
            ref Dictionary<MethodInfo, ParameterInfo[]> parametersByMethod)
        {
            foreach (var methodInfo in methods)
            {
                if (parametersByMethod.TryGetValue(methodInfo, out var parameters))
                {
                    var dependencies = new object[parameters.Length];

                    for (var i = 0; i < parameters.Length; i++)
                    {
                        var parameter = parameters[i];
                        var dependency = Resolve(parameter.ParameterType);
                        dependencies[i] = dependency;
                    }

                    methodInfo.Invoke(targetObject, dependencies);
                }
                else
                {
                    methodInfo.Invoke(targetObject, null);
                }
            }
        }
    }
}
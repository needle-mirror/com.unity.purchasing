#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This is a lightweight dependency injector. You can add dependencies to it using the AddService or AddInstance methods, and the
    /// GetService method should initialize the appropriate Service if necessary and return it.
    /// If a service has multiple constructors, use the [Inject] attribute to select the one you wish to use.
    /// </summary>
    class DependencyInjectionService : IDependencyInjectionService
    {
        readonly HashSet<object> m_ServiceInstances = new HashSet<object>();
        readonly HashSet<Type> m_ServiceConcreteTypes = new HashSet<Type>();

        public void AddInstance(object instance)
        {
            m_ServiceInstances.Add(instance);
        }

        public void AddService<T>() where T : class
        {
            var type = typeof(T);
            ValidateTypeIsNotAnInterface(type);
            ValidateTypeIsNotADuplicate(type);

            m_ServiceConcreteTypes.Add(type);
        }

        void ValidateTypeIsNotAnInterface(Type type)
        {
            if (type.IsInterface)
            {
                throw new DependencyInjectionException($"Cannot use interface as service type {type}");
            }
        }

        void ValidateTypeIsNotADuplicate(Type type)
        {
            if (m_ServiceConcreteTypes.Contains(type))
            {
                throw new DependencyInjectionException($"Service of type {type} is already present");
            }
        }

        public T GetInstance<T>() where T : class
        {
            var type = typeof(T);
            return (GetInstance(type) as T)!;
        }

        object GetInstance(Type type)
        {
            return FindServiceInstance(type) ??
                   CreateServiceInstance(FindConcreteServiceType(type));
        }

        object? FindServiceInstance(Type type)
        {
            var validInstances = m_ServiceInstances.Where(type.IsInstanceOfType)
                .ToList();

            if (validInstances.Count > 1)
            {
                throw new DependencyInjectionException($"Multiple instances found for the type: {type}");
            }

            return validInstances.FirstOrDefault();
        }

        Type FindConcreteServiceType(Type type)
        {
            var validTypes = m_ServiceConcreteTypes.Where(type.IsAssignableFrom).ToList();

            if (validTypes.Count > 1)
            {
                throw new DependencyInjectionException($"Multiple possible assignable types found for the type: {type}");
            }
            if (!validTypes.Any())
            {
                throw new DependencyInjectionException($"No possible assignable types found for the type: {type}");
            }

            return validTypes.First();
        }

        object CreateServiceInstance(Type concreteType)
        {
            var constructor = FindConstructorForType(concreteType);

            var parameterInstances = GetParameterInstancesForConstructor(constructor);

            var instance = constructor.Invoke(parameterInstances);
            AddInstance(instance);

            return instance;
        }

        ConstructorInfo FindConstructorForType(Type concreteType)
        {
            var constructors =
                concreteType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (constructors.Length == 0)
            {
                throw new DependencyInjectionException($"Service {concreteType} has no visible constructor from this assembly.");
            }

            return constructors.Length == 1
                ? constructors.First()
                : HandleMultipleConstructors(constructors, concreteType);
        }

        ConstructorInfo HandleMultipleConstructors(ConstructorInfo[] constructors, Type concreteType)
        {
            var injectableConstructors = FindInjectableConstructors(constructors);
            if (injectableConstructors.Count == 0)
            {
                throw new DependencyInjectionException(
                    $"Service {concreteType} has multiple constructors, select one by adding the [Inject] attribute to it.");
            }

            if (injectableConstructors.Count > 1)
            {
                throw new DependencyInjectionException(
                    $"Service {concreteType} has multiple injectable constructors, the class must have only one constructor with the [Inject] attribute.");
            }

            return injectableConstructors.First();
        }

        List<ConstructorInfo> FindInjectableConstructors(ConstructorInfo[] constructors)
        {
            return constructors.Where(constructor =>
                constructor.CustomAttributes.Any(attribute => attribute.AttributeType == typeof(InjectAttribute))).ToList();
        }

        object[] GetParameterInstancesForConstructor(ConstructorInfo constructor)
        {
            var parameterTypes = constructor.GetParameters().Select(paramInfo => paramInfo.ParameterType);
            return parameterTypes.Select(GetInstance).ToArray();
        }
    }
}

// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Unity.Purchasing.Editor.Shared.DependencyInversion
{
    class ServiceCollection
    {
        public Dictionary<Type, IServiceFactory> Factories = new Dictionary<Type, IServiceFactory>();

        protected internal List<Type> m_StartupTypes = new List<Type>();

        readonly Scope m_GlobalScope;

        public ServiceCollection(bool strict = false)
        {
            m_GlobalScope = new Scope(strict);
        }

        public void Register<T>(Func<IServiceProvider, T> factory) where T : class
        {
            if (Factories.ContainsKey(typeof(T)))
            {
                throw new TypeAlreadyRegisteredException(typeof(T));
            }

            Factories[typeof(T)] = new Transient(factory, m_GlobalScope);
        }

        public void RegisterSingleton<T>(Func<IServiceProvider, T> factory) where T : class
        {
            if (Factories.ContainsKey(typeof(T)))
            {
                throw new TypeAlreadyRegisteredException(typeof(T));
            }

            var singleton = new Singleton(factory, m_GlobalScope);
            Factories[typeof(T)] = singleton;
        }

        public void RegisterStartupSingleton<T>(Func<IServiceProvider, T> factory) where T : class
        {
            RegisterSingleton(factory);
            m_StartupTypes.Add(typeof(T));
        }

        public IScopedServiceProvider Build()
        {
            var provider = new ServiceProvider(new ReadOnlyDictionary<Type, IServiceFactory>(Factories), m_GlobalScope);

            foreach (var startupType in m_StartupTypes)
            {
                provider.GetService(startupType);
            }

            return provider;
        }
    }
}

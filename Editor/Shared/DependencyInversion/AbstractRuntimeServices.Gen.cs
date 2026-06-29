// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;

namespace Unity.Purchasing.Editor.Shared.DependencyInversion
{
    abstract class AbstractRuntimeServices<T> where T : AbstractRuntimeServices<T>, new()
    {
        IScopedServiceProvider m_ServiceProvider;
        static T s_Instance;

        public static T Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = new T();
                }

                return s_Instance;
            }
        }

        public ServiceCollection Collection { get; private set; }

        protected AbstractRuntimeServices()
        {
            if (s_Instance != null)
            {
                throw new Exception(
                    $"Runtime Services of type {typeof(T)} can only be instantiated once. Try to using Instance property.");
            }
        }

        public void Initialize(ServiceCollection collection, bool register = true)
        {
            Collection = collection;

            if (register)
            {
                Register(collection);
            }

            m_ServiceProvider = Collection.Build();
        }

        public IScopedServiceProvider CreateScope()
        {
            return m_ServiceProvider.CreateScope();
        }

        public TService GetService<TService>()
        {
            return (TService)m_ServiceProvider.GetService(typeof(TService));
        }

        public IDisposable InitializeInstance<TService>(TService instance)
        {
            var scope = m_ServiceProvider.CreateScope();
            Factories.InitializeInstance(scope, instance);
            return scope;
        }

        public abstract void Register(ServiceCollection collection);
    }
}

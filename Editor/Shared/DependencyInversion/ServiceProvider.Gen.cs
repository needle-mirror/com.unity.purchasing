// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;
using System.Collections.Generic;

namespace Unity.Purchasing.Editor.Shared.DependencyInversion
{
    class ServiceProvider : IScopedServiceProvider
    {
        readonly IReadOnlyDictionary<Type, IServiceFactory> m_Factories;
        readonly Scope m_Scope;

        public ServiceProvider(
            IReadOnlyDictionary<Type, IServiceFactory> factories,
            Scope scope
        )
        {
            m_Factories = factories;
            m_Scope = scope;
        }

        public object GetService(Type serviceType)
        {
            if (m_Factories.TryGetValue(serviceType, out var factory))
            {
                return GetService(factory);
            }
            throw new DependencyNotFoundException(serviceType);
        }

        public IScopedServiceProvider CreateScope()
        {
            return new ServiceProvider(m_Factories, new Scope());
        }

        object GetService(IServiceFactory factory)
        {
            try
            {
                return factory.Build(this, m_Scope);
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to build '{factory}'", e);
            }
        }

        public void Dispose()
        {
            m_Scope.Dispose();
        }
    }
}

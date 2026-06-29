// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;

namespace Unity.Purchasing.Editor.Shared.DependencyInversion
{
    class Singleton : IServiceFactory
    {
        object m_Instance;
        readonly Func<IServiceProvider, object> m_Factory;
        readonly Scope m_GlobalScope;

        public Singleton(Func<IServiceProvider, object> factory, Scope globalScope)
        {
            m_Factory = factory;
            m_GlobalScope = globalScope;
        }

        public object Build(IServiceProvider provider, Scope _)
        {
            if (m_Instance == null)
            {
                m_Instance = m_Factory(provider);
                if (m_Instance is IDisposable disposableInst)
                {
                    m_GlobalScope.Add(disposableInst);
                }
            }
            return m_Instance;
        }

        public override string ToString()
        {
            return $"{m_Factory.GetType().GenericTypeArguments[1].Name} {GetType().Name}";
        }
    }
}

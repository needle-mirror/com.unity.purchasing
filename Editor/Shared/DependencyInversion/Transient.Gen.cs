// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;

namespace Unity.Purchasing.Editor.Shared.DependencyInversion
{
    class Transient : IServiceFactory
    {
        readonly Func<IServiceProvider, object> m_Factory;
        readonly Scope m_GlobalScope;

        public Transient(Func<IServiceProvider, object> factory, Scope globalScope)
        {
            m_Factory = factory;
            m_GlobalScope = globalScope;
        }

        public object Build(IServiceProvider provider, Scope scope)
        {
            var inst = m_Factory(provider);
            if (inst is IDisposable disposableInst)
            {
                if (scope == m_GlobalScope && m_GlobalScope.Strict)
                {
                    throw new InvalidOperationException($"IDisposable Transient service {inst.GetType().Name} was created in the global scope, this will not be disposed and will result in memory leak.");
                }
                scope.Add(disposableInst);
            }
            return inst;
        }

        public override string ToString()
        {
            return $"{m_Factory.GetType().GenericTypeArguments[1].Name} {GetType().Name}";
        }
    }
}

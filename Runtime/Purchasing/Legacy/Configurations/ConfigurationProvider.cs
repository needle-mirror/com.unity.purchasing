using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    class ConfigurationProvider
    {
        readonly Dictionary<Type, IStoreConfiguration> m_ConfigMap = new Dictionary<Type, IStoreConfiguration>();

        public ConfigurationProvider()
        {
            RegisterConfigurations();
        }

        public T GetConfiguration<T>() where T : IStoreConfiguration
        {
            var t = typeof(T);
            if (m_ConfigMap.ContainsKey(t))
            {
                return (T)m_ConfigMap[t];
            }

            throw new ArgumentException("No binding for type " + t);
        }

        void RegisterConfigurations()
        {
            m_ConfigMap[typeof(IAppleConfiguration)] = new AppleConfiguration();
            m_ConfigMap[typeof(IGooglePlayConfiguration)] = new GooglePlayConfiguration();
        }
    }
}

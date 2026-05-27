using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    class ConfigurationProvider
    {
// Obsolete: IStoreConfiguration
#pragma warning disable 618, 612
        readonly Dictionary<Type, IStoreConfiguration> m_ConfigMap = new Dictionary<Type, IStoreConfiguration>();
#pragma warning restore 618, 612

        public ConfigurationProvider()
        {
            RegisterConfigurations();
        }

// Obsolete: IStoreConfiguration
#pragma warning disable 618, 612
        public T GetConfiguration<T>() where T : IStoreConfiguration
#pragma warning restore 618, 612
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
// Obsolete: IAppleConfiguration, IGooglePlayConfiguration
#pragma warning disable 618, 612
            m_ConfigMap[typeof(IAppleConfiguration)] = new AppleConfiguration();
            m_ConfigMap[typeof(IGooglePlayConfiguration)] = new GooglePlayConfiguration();
#pragma warning restore 618, 612
        }
    }
}

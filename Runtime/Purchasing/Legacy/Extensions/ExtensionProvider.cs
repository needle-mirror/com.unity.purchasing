using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Purchasing
{
    class ExtensionProvider : IExtensionProvider
    {
        readonly Dictionary<Type, IStoreExtension> m_ExtensionMap = new Dictionary<Type, IStoreExtension>();

        public ExtensionProvider()
        {
            RegisterExtensions();
        }

        void RegisterExtensions()
        {
            m_ExtensionMap[typeof(IAppleExtensions)] = new AppleExtensions();
            m_ExtensionMap[typeof(IGooglePlayStoreExtensions)] = new GoogleExtensions();
        }

        public T GetExtension<T>() where T : IStoreExtension
        {
            var t = typeof(T);
            if (m_ExtensionMap.ContainsKey(t))
            {
                return (T)m_ExtensionMap[t];
            }

            throw new ArgumentException("No binding for type " + t);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEditor;

namespace UnityEngine.Purchasing
{
// Obsolete: IExtensionProvider
#pragma warning disable 618, 612
    class ExtensionProvider: IExtensionProvider
#pragma warning restore 618, 612
    {
// Obsolete: IStoreExtension
#pragma warning disable 618, 612
        readonly Dictionary<Type, IStoreExtension> m_ExtensionMap = new Dictionary<Type, IStoreExtension>();
#pragma warning restore 618, 612

        public ExtensionProvider()
        {
            RegisterExtensions();
        }

        void RegisterExtensions()
        {
// Obsolete: IAppleExtensions, IGooglePlayStoreExtensions
#pragma warning disable 618, 612
            m_ExtensionMap[typeof(IAppleExtensions)] = new AppleExtensions();
            m_ExtensionMap[typeof(IGooglePlayStoreExtensions)] = new GoogleExtensions();
#pragma warning restore 618, 612
        }

// Obsolete: IStoreExtension
#pragma warning disable 618, 612
        public T GetExtension<T>() where T : IStoreExtension
#pragma warning restore 618, 612
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

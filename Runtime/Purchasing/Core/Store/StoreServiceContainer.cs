#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Container class that houses a StoreService created through dependency injection.
    /// </summary>
    class StoreServiceContainer
    {
        static StoreServiceContainer? s_Instance;

        readonly Dictionary<string?, IStoreService> m_InstantiatedServices = new();

        internal static StoreServiceContainer Instance()
        {
            return s_Instance ??= new StoreServiceContainer();
        }

        internal IStoreService? FindService(string storeName)
        {
            return !m_InstantiatedServices.ContainsKey(storeName) ? null : m_InstantiatedServices[storeName];
        }

        internal void SetService(string storeName, IStoreService service)
        {
            if (m_InstantiatedServices.ContainsKey(storeName))
            {
                throw new ServiceCreationException($"StoreService for store {storeName} already exists. Cannot create a new instance.");
            }

            m_InstantiatedServices.Add(storeName, service);
        }
    }
}

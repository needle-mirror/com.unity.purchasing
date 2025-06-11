#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Container class that houses a PurchasingService created through dependency injection.
    /// </summary>
    class PurchaseServiceContainer
    {
        static PurchaseServiceContainer? s_Instance;

        readonly Dictionary<string?, IPurchaseService> m_InstantiatedServices = new Dictionary<string?, IPurchaseService>();

        internal static PurchaseServiceContainer Instance()
        {
            return s_Instance ??= new PurchaseServiceContainer();
        }

        internal IPurchaseService? FindService(string storeName)
        {
            if (!m_InstantiatedServices.ContainsKey(storeName))
            {
                return null;
            }

            return m_InstantiatedServices[storeName];
        }

        internal void SetService(string storeName, IPurchaseService service)
        {
            if (m_InstantiatedServices.ContainsKey(storeName))
            {
                throw new ServiceCreationException($"PurchaseService for store {storeName} already exists. Cannot create a new instance.");
            }

            m_InstantiatedServices.Add(storeName, service);
        }
    }
}

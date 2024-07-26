#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Container class that houses a ProductService created through dependency injection.
    /// </summary>
    class ProductServiceContainer
    {
        static ProductServiceContainer? s_Instance;

        readonly Dictionary<string, IProductService> m_InstantiatedServices = new Dictionary<string, IProductService>();

        internal static ProductServiceContainer Instance()
        {
            return s_Instance ??= new ProductServiceContainer();
        }

        internal IProductService? FindService(string storeName)
        {
            if (!m_InstantiatedServices.ContainsKey(storeName))
            {
                return null;
            }

            return m_InstantiatedServices[storeName];
        }

        internal void SetService(string storeName, IProductService service)
        {
            if (m_InstantiatedServices.ContainsKey(storeName))
            {
                throw new ServiceCreationException($"ProductService for store {storeName} already exists. Cannot create a new instance.");
            }

            m_InstantiatedServices.Add(storeName, service);
        }
    }
}

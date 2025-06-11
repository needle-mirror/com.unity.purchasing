using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Manages instantiation of specific store services based on provided <c>IPurchasingModule</c>s.
    /// </summary>
    [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
    internal class PurchasingFactory
    {
        private IStore m_Store;
        private IBaseCatalogProvider m_CatalogProvider;

        public string storeName { get; private set; }

        public IStore service
        {
            get
            {
                if (m_Store != null)
                {
                    return m_Store;
                }

                throw new InvalidOperationException("No impl available!");
            }

            set => m_Store = value;
        }

        public void RegisterStore(string name, Store s)
        {
            // We use the first store that supports our current
            // platform.
            if (m_Store == null && s != null)
            {
                storeName = name;
                service = s;
            }
        }

        public void SetCatalogProvider(IBaseCatalogProvider provider)
        {
            m_CatalogProvider = provider;
        }

        public void SetCatalogProviderFunction(Action<Action<List<ProductDefinition>>> func)
        {
            m_CatalogProvider = new SimpleCatalogProvider(func);
        }

        internal IBaseCatalogProvider GetCatalogProvider()
        {
            return m_CatalogProvider;
        }
    }
}

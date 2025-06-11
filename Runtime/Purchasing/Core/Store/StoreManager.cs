#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    class StoreManager : IStoreManager, IStoreManagerFactoryInjectionPoint
    {
        static StoreManager? s_Instance;

        IStoreFactory? m_StoreFactory;
        readonly Dictionary<string?, IStoreWrapper> m_InstantiatedStores = new Dictionary<string?, IStoreWrapper>();

        public void SetStoreFactory(IStoreFactory? storeFactory)
        {
            m_StoreFactory = storeFactory;
        }

        public static StoreManager Instance()
        {
            return s_Instance ??= new StoreManager();
        }

        public IStoreWrapper GetStore(string name)
        {
            if (!m_InstantiatedStores.ContainsKey(name))
            {
                if (m_StoreFactory == null)
                {
                    throw new StoreException("Store Factory not present to create any new stores");
                }

                var store = m_StoreFactory.CreateStore(name);
                m_InstantiatedStores[name] = store;
            }

            return m_InstantiatedStores[name];
        }

        public IStoreWrapper GetDefaultStore()
        {
            return GetStore(DefaultStoreHelper.GetDefaultStoreName());
        }

        public void AddNewCustomStore(IStoreWrapper customStore)
        {
            if (m_StoreFactory == null)
            {
                throw new StoreException("Store Factory not present to create any new stores");
            }

            IStoreWrapper InstantiateCustomStore()
            {
                return customStore;
            }

            m_StoreFactory.RegisterStore(customStore.name, InstantiateCustomStore);
        }
    }
}

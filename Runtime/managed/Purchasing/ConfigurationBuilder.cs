using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    ///  Maps store specific Product identifiers to one
    /// or more store identifiers.
    ///
    /// The name is deliberately terse for use as a collection initializer.
    /// </summary>
    public class IDs : IEnumerable<KeyValuePair<string, string>>
    {
        private Dictionary<string, string> m_Dic = new Dictionary<string, string>();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Dic.GetEnumerator();
        }

        public void Add(string id, params string[] stores)
        {
            foreach (var store in stores)
                m_Dic[store] = id;
        }

        /// <summary>
        /// Allow definition of store names with non strings such as Enums.
        /// </summary>
        public void Add(string id, params object[] stores)
        {
            foreach (var store in stores)
                m_Dic[store.ToString()] = id;
        }

        internal string SpecificIDForStore(string store, string defaultValue)
        {
            if (m_Dic.ContainsKey(store))
                return m_Dic[store];
            return defaultValue;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return m_Dic.GetEnumerator();
        }
    }

    /// <summary>
    /// Builds configuration for Unity Purchasing,
    /// consisting of products and store specific configuration details.
    /// </summary>
    public class ConfigurationBuilder
    {
        private PurchasingFactory m_Factory;
        private HashSet<ProductDefinition> m_Products = new HashSet<ProductDefinition>();

        internal ConfigurationBuilder(PurchasingFactory factory)
        {
            m_Factory = factory;
        }

		[Obsolete("This property has been renamed 'useCatalogProvider'", false)]
        public bool useCloudCatalog
        {
            get;
            set;
        }

		public bool useCatalogProvider
		{
			get;
			set;
		}

        public HashSet<ProductDefinition> products
        {
            get { return m_Products; }
        }

        internal PurchasingFactory factory
        {
            get { return m_Factory; }
        }

        public T Configure<T>() where T : IStoreConfiguration
        {
            return m_Factory.GetConfig<T>();
        }

        public static ConfigurationBuilder Instance(IPurchasingModule first, params IPurchasingModule[] rest)
        {
            PurchasingFactory factory = new PurchasingFactory(first, rest);
            return new ConfigurationBuilder(factory);
        }

        public ConfigurationBuilder AddProduct(string id, ProductType type)
        {
            return AddProduct(id, type, null);
        }

        public ConfigurationBuilder AddProduct(string id, ProductType type, IDs storeIDs)
        {
            return AddProduct(id, type, storeIDs, (IEnumerable<PayoutDefinition>)null);
        }

        public ConfigurationBuilder AddProduct(string id, ProductType type, IDs storeIDs, PayoutDefinition payout)
        {
            return AddProduct(id, type, storeIDs, new List<PayoutDefinition> { payout });
        }

        public ConfigurationBuilder AddProduct(string id, ProductType type, IDs storeIDs, IEnumerable<PayoutDefinition> payouts)
        {
            var specificId = id;
            // Extract our store specific ID if present, according to the current store.
            if (storeIDs != null)
                specificId = storeIDs.SpecificIDForStore(factory.storeName, id);
            var product = new ProductDefinition(id, specificId, type);
            product.SetPayouts(payouts);
            m_Products.Add(product);

            return this;
        }

        public ConfigurationBuilder AddProducts(IEnumerable<ProductDefinition> products)
        {
            foreach (var product in products) {
                m_Products.Add(product);
            }

            return this;
        }
    }
}

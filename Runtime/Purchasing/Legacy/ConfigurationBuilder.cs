using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Builds configuration for Unity Purchasing,
    /// consisting of products and store specific configuration details.
    /// </summary>
    [Obsolete("Please upgrade to the new APIs available. For more info visit `Upgrading to IAP v5` in the IAP documentation. https://docs.unity3d.com/Packages/com.unity.purchasing@latest", false)]
    public class ConfigurationBuilder
    {
        static ConfigurationBuilder instance = null;
        /// <summary>
        /// Create an instance of the configuration builder.
        /// </summary>
        /// <param name="first"> The first purchasing module. </param>
        /// <param name="rest"> The remaining purchasing modules, excluding the one passes as first. </param>
        /// <returns> The instance of the configuration builder as specified. </returns>
        public static ConfigurationBuilder Instance(object ignored)
        {
            if (instance == null)
            {
                instance = new ConfigurationBuilder();
            }
            return instance;
        }

        internal CatalogProvider m_CatalogProvider = new CatalogProvider();
        internal ConfigurationProvider m_ConfigurationProvider = new ConfigurationProvider();

        /// <summary>
        /// Add a product to the configuration builder.
        /// </summary>
        /// <param name="id"> The id of the product. </param>
        /// <param name="type"> The type of the product. </param>
        /// <returns> The instance of the configuration builder with the new product added. </returns>
        public void AddProduct(string id, ProductType type)
        {
            AddProduct(id, type, null);
        }

        /// <summary>
        /// Add a product to the configuration builder.
        /// </summary>
        /// <param name="id"> The id of the product. </param>
        /// <param name="type"> The type of the product. </param>
        /// <param name="storeIDs"> The object representing store IDs the product is to be added to. </param>
        /// <returns> The instance of the configuration builder with the new product added. </returns>
        public void AddProduct(string id, ProductType type, StoreSpecificIds storeIDs)
        {
            AddProduct(id, type, storeIDs, (IEnumerable<PayoutDefinition>)null!);
        }

        /// <summary>
        /// Add a product to the configuration builder.
        /// </summary>
        /// <param name="id"> The id of the product. </param>
        /// <param name="type"> The type of the product. </param>
        /// <param name="storeIDs"> The object representing store IDs the product is to be added to. </param>
        /// <param name="payout"> The payout definition of the product. </param>
        /// <returns> The instance of the configuration builder with the new product added. </returns>
        public void AddProduct(string id, ProductType type, StoreSpecificIds storeIDs, PayoutDefinition payout)
        {
            AddProduct(id, type, storeIDs, new List<PayoutDefinition> { payout });
        }

        /// <summary>
        /// Add a product to the configuration builder.
        /// </summary>
        /// <param name="id"> The id of the product. </param>
        /// <param name="type"> The type of the product. </param>
        /// <param name="storeIDs"> The object representing store IDs the product is to be added to. </param>
        /// <param name="payouts"> The enumerator of the payout definitions of the product. </param>
        /// <returns> The instance of the configuration builder with the new product added. </returns>
        public void AddProduct(string id, ProductType type, StoreSpecificIds storeIDs, IEnumerable<PayoutDefinition> payouts)
        {
            m_CatalogProvider.AddProduct(id, type, storeIDs, payouts);
        }

        /// <summary>
        /// Configure the store as specified by the template parameter.
        /// </summary>
        /// <typeparam name="T"> Implementation of <c>IStoreConfiguration</c> </typeparam>
        /// <returns> The store configuration as an object. </returns>
        public T Configure<T>() where T : IStoreConfiguration
        {
            return m_ConfigurationProvider.GetConfiguration<T>();
        }

    }
}

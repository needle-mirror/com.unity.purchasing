using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Builds configuration for Unity Purchasing,
    /// consisting of products and store specific configuration details.
    /// </summary>
    [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
    public class ConfigurationBuilder
    {
        static ConfigurationBuilder instance = null;
        /// <summary>
        /// Create an instance of the configuration builder.
        /// </summary>
        /// <param name="ignored"> No longer used parameter. </param>
        /// <returns> The instance of the configuration builder as specified. </returns>
        /// <remarks>
        /// Starting from IAP 5.0.0, this is now a singleton.
        /// This improves internal persistence across initializations, but repeated calls to
        /// UnityPurchasing.Initialize() using the same builder instance may result in duplicate product registration errors ("Same key").
        ///
        /// If your use case requires a fresh configuration, consider using the public constructor:
        ///     new ConfigurationBuilder(new PurchasingFactory());
        ///
        /// However, this constructor may not remain public in future versions.
        /// It's recommended to avoid reinitializing IAP unless absolutely necessary.
        /// </remarks>
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

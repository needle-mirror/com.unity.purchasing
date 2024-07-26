#nullable enable
using System.Collections.Generic;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Services
{
    class AmazonAppsExtendedProductService : ProductService, IAmazonAppsExtendedProductService
    {
        IAmazonAppsNotifyUnableToFulfillUseCase m_AppsNotifyUnableToFulfillUseCase;
        IAmazonAppStoreWriteSandboxJsonUseCase m_AmazonAppStoreWriteSandboxJsonUseCase;

        [Preserve]
        internal AmazonAppsExtendedProductService(
            IAmazonAppsNotifyUnableToFulfillUseCase notifyNotifyUnableToFulfillUseCase,
            IAmazonAppStoreWriteSandboxJsonUseCase writeSandboxJsonUseCaseUseCase,
            IFetchProductsUseCase fetchProductsUseCase,
            IStoreWrapper storeWrapper)
            : base(fetchProductsUseCase, storeWrapper)
        {
            m_AppsNotifyUnableToFulfillUseCase = notifyNotifyUnableToFulfillUseCase;
            m_AmazonAppStoreWriteSandboxJsonUseCase = writeSandboxJsonUseCaseUseCase;
        }

        /// <summary>
        /// Amazon makes it possible to notify them of a product that cannot be fulfilled.
        ///
        /// This method calls Amazon's notifyFulfillment(transactionID, FulfillmentResult.UNAVAILABLE);
        /// https://developer.amazon.com/public/apis/earn/in-app-purchasing/docs-v2/implementing-iap-2.0
        /// </summary>
        /// <param name="transactionID">Products transaction id</param>
        public void NotifyUnableToFulfillUnavailableProduct(string transactionID)
        {
            m_AppsNotifyUnableToFulfillUseCase.NotifyUnableToFulfillUnavailableProduct(transactionID);
        }

        /// <summary>
        /// To use for Amazon’s local Sandbox testing app, generate a JSON description of your product catalog on the device’s SD card.
        /// </summary>
        /// <param name="products">Products to add to the testing app JSON.</param>
        public void WriteSandboxJSON(HashSet<ProductDefinition> products)
        {
            m_AmazonAppStoreWriteSandboxJsonUseCase.WriteSandboxJSON(products);
        }
    }
}

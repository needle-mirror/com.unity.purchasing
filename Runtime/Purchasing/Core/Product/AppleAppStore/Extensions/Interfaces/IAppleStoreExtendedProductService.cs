using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A public interface for the Apple Store product service extension.
    /// </summary>
    public interface IAppleStoreExtendedProductService : IProductServiceExtension
    {
        /// <summary>
        /// Returns the current promoted product order on the device
        /// </summary>
        /// <param name="successCallback">This action will be called when the fetch is successful. The list of products will be passed through.</param>
        /// <param name="errorCallback">This action will be called when the fetch is in error. The error will be passed through.</param>
        void FetchStorePromotionOrder(Action<List<Product>> successCallback, Action<string> errorCallback);

        /// <summary>
        /// Overrides the promoted product order on the device.
        /// </summary>
        /// <param name="products">The new order of promoted products for the device.</param>
        void SetStorePromotionOrder(List<Product> products);

        /// <summary>
        /// Returns the current promoted product order on the device
        /// </summary>
        /// <param name="product">Product to change visibility.</param>
        /// <param name="successCallback">This action will be called when the fetch is successful. The productId and visibility will be passed through.</param>
        /// <param name="errorCallback">This action will be called when the fetch is in error. The error will be passed through.</param>
        void FetchStorePromotionVisibility(Product product, Action<string, AppleStorePromotionVisibility> successCallback, Action<string> errorCallback);

        /// <summary>
        /// Override the visibility of a product on the device.
        /// </summary>
        /// <param name="product">Product to change visibility.</param>
        /// <param name="visible">The new product visibility.</param>
        void SetStorePromotionVisibility(Product product, AppleStorePromotionVisibility visible);

        /// <summary>
        /// Extracting Introductory Price subscription related product details.
        /// </summary>
        /// <returns>returns the Introductory Price subscription related product details or an empty dictionary</returns>
        Dictionary<string, string> GetIntroductoryPriceDictionary();

        /// <summary>
        /// Extracting product details from the last successful FetchProducts request.
        /// </summary>
        /// <returns>returns product details or an empty dictionary</returns>
        Dictionary<string, string> GetProductDetails();
    }
}

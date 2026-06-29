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
        /// Overrides the promoted catalog listings order on the device.
        /// </summary>
        /// <param name="catalogListingIds">The new order of promoted catalog listings for the device.</param>
        void SetStorePromotionOrder(List<string> catalogListingIds);

        /// <summary>
        /// Convenience wrapper that derives the catalog listing id from <c>product.baseListing.id</c>
        /// and calls <see cref="FetchStorePromotionVisibility(string, Action{string, AppleStorePromotionVisibility}, Action{string})"/>.
        /// Prefer the catalogListingId overload directly when you know which catalog listing to target — it is the canonical entry point for multi-listing scenarios.
        /// </summary>
        /// <param name="product">Product whose storefront promotion visibility should be fetched.</param>
        /// <param name="successCallback">This action will be called when the fetch is successful. The productId and visibility will be passed through.</param>
        /// <param name="errorCallback">This action will be called when the fetch is in error. The error will be passed through.</param>
        void FetchStorePromotionVisibility(Product product, Action<string, AppleStorePromotionVisibility> successCallback, Action<string> errorCallback);

        /// <summary>
        /// Fetches the current storefront promotion visibility for the product that owns the given catalog listing.
        /// This is the canonical overload — the <see cref="FetchStorePromotionVisibility(Product, Action{string, AppleStorePromotionVisibility}, Action{string})"/> overload forwards here.
        /// </summary>
        /// <param name="catalogListingId">Catalog listing id whose owning product's storefront promotion visibility should be fetched.</param>
        /// <param name="successCallback">Called when the fetch is successful. The productId and visibility will be passed through.</param>
        /// <param name="errorCallback">Called when the fetch fails or no product matches <paramref name="catalogListingId"/>. The error message will be passed through.</param>
        void FetchStorePromotionVisibility(string catalogListingId, Action<string, AppleStorePromotionVisibility> successCallback, Action<string> errorCallback);

        /// <summary>
        /// Convenience wrapper that derives the catalog listing id from <c>product.baseListing.id</c>
        /// and calls <see cref="SetStorePromotionVisibility(string, AppleStorePromotionVisibility)"/>.
        /// Prefer the catalogListingId overload directly when you know which catalog listing to target — it is the canonical entry point for multi-listing scenarios.
        /// </summary>
        /// <param name="product">Product whose storefront promotion visibility should be set.</param>
        /// <param name="visible">The new product visibility.</param>
        void SetStorePromotionVisibility(Product product, AppleStorePromotionVisibility visible);

        /// <summary>
        /// Overrides the storefront promotion visibility for the product that owns the given catalog listing.
        /// This is the canonical overload — the <see cref="SetStorePromotionVisibility(Product, AppleStorePromotionVisibility)"/> overload forwards here.
        /// </summary>
        /// <param name="catalogListingId">Catalog listing id whose owning product's storefront promotion visibility should be set.</param>
        /// <param name="visible">The new product visibility.</param>
        void SetStorePromotionVisibility(string catalogListingId, AppleStorePromotionVisibility visible);

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

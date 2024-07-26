#nullable enable

using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePurchaseService : IGooglePurchaseService
    {
        readonly IGoogleBillingClient m_BillingClient;
        readonly IGooglePurchaseCallback m_GooglePurchaseCallback;
        readonly IQueryProductDetailsService m_QueryProductDetailsService;
        readonly ILogger m_Logger;
        IProductCache? m_ProductCache;

        [Preserve]
        internal GooglePurchaseService(IGoogleBillingClient billingClient, IGooglePurchaseCallback googlePurchaseCallback, IQueryProductDetailsService queryProductDetailsService, ILogger logger)
        {
            m_BillingClient = billingClient;
            m_GooglePurchaseCallback = googlePurchaseCallback;
            m_QueryProductDetailsService = queryProductDetailsService;
            m_Logger = logger;
        }

        public async void Purchase(ProductDefinition product, Product? oldProduct, GooglePlayProrationMode? desiredProrationMode)
        {
            var productDetailsList = await m_QueryProductDetailsService.QueryProductDetails(product);
            OnQueryProductDetailsResponse(productDetailsList, product, oldProduct, desiredProrationMode);
        }

        void OnQueryProductDetailsResponse(List<AndroidJavaObject> productDetailsList, ProductDefinition productToBuy, Product? oldProduct, GooglePlayProrationMode? desiredProrationMode)
        {
            if (ValidateQueryProductDetailsResponseParams(productDetailsList, productToBuy, oldProduct))
            {
                LaunchGoogleBillingFlow(productDetailsList[0], oldProduct, desiredProrationMode);
            }
        }

        bool ValidateQueryProductDetailsResponseParams(List<AndroidJavaObject> skus, ProductDefinition productToBuy, Product? oldProduct)
        {
            if (!ValidateSkus(skus))
            {
                PurchaseFailedSkuNotFound(productToBuy);
                return false;
            }

            if (!ValidateOldProduct(oldProduct))
            {
                PurchaseFailedInvalidOldProduct(productToBuy, oldProduct);
                return false;
            }

            return true;
        }

        bool ValidateSkus(List<AndroidJavaObject>? skus)
        {
            VerifyAndWarnIfMoreThanOneSku(skus);
            return skus?.Count > 0;
        }

        void VerifyAndWarnIfMoreThanOneSku(List<AndroidJavaObject>? skus)
        {
            if (skus?.Count > 1)
            {
                m_Logger.LogIAPWarning(GoogleBillingStrings.getWarningMessageMoreThanOneSkuFound(
                    skus[0].Call<string>("getProductId")));
            }
        }

        void PurchaseFailedSkuNotFound(ProductDefinition productToBuy)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    m_ProductCache?.FindOrDefault(productToBuy.id) ?? Product.CreateUnknownProduct(productToBuy.id),
                    PurchaseFailureReason.ProductUnavailable,
                    "SKU does not exist in the store."
                )
            );
        }

        [System.Obsolete]
        bool ValidateOldProduct(Product? oldProduct)
        {
            return oldProduct?.transactionID != "";
        }

        void PurchaseFailedInvalidOldProduct(ProductDefinition productToBuy, Product? oldProduct)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    m_ProductCache?.FindOrDefault(productToBuy.id) ?? Product.CreateUnknownProduct(productToBuy.id),
                    PurchaseFailureReason.ProductUnavailable,
                    "Invalid transaction id for old product: " + oldProduct?.definition.id
                )
            );
        }

        [System.Obsolete]
        void LaunchGoogleBillingFlow(AndroidJavaObject productToPurchase, Product? oldProduct, GooglePlayProrationMode? desiredProrationMode)
        {
            var billingResult = m_BillingClient.LaunchBillingFlow(productToPurchase, oldProduct?.transactionID, desiredProrationMode);
            HandleBillingFlowResult(new GoogleBillingResult(billingResult), productToPurchase);
        }

        void HandleBillingFlowResult(IGoogleBillingResult billingResult, AndroidJavaObject sku)
        {
            if (billingResult.responseCode != GoogleBillingResponseCode.Ok)
            {
                var productId = sku.Call<string>("getProductId");
                m_GooglePurchaseCallback.OnPurchaseFailed(
                    new PurchaseFailureDescription(
                        m_ProductCache?.FindOrDefault(productId) ?? Product.CreateUnknownProduct(productId),
                        PurchaseFailureReason.PurchasingUnavailable,
                        billingResult.debugMessage
                    )
                );
            }
        }

        public void SetProductCache(IProductCache? productCache)
        {
            m_ProductCache = productCache;
        }
    }
}

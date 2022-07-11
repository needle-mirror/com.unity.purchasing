#nullable enable

using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing
{
    class GooglePurchaseService : IGooglePurchaseService
    {
        IGoogleBillingClient m_BillingClient;
        IGooglePurchaseCallback m_GooglePurchaseCallback;
        IQuerySkuDetailsService m_QuerySkuDetailsService;

        internal GooglePurchaseService(IGoogleBillingClient billingClient, IGooglePurchaseCallback googlePurchaseCallback, IQuerySkuDetailsService querySkuDetailsService)
        {
            m_BillingClient = billingClient;
            m_GooglePurchaseCallback = googlePurchaseCallback;
            m_QuerySkuDetailsService = querySkuDetailsService;
        }

        public void Purchase(ProductDefinition product, Product? oldProduct, GooglePlayProrationMode? desiredProrationMode)
        {
            m_QuerySkuDetailsService.QueryAsyncSku(product,
                skus =>
                {
                    OnQuerySkuDetailsResponse(skus, product, oldProduct, desiredProrationMode);
                });
        }

        void OnQuerySkuDetailsResponse(List<AndroidJavaObject> skus, ProductDefinition productToBuy, Product? oldProduct, GooglePlayProrationMode? desiredProrationMode)
        {
            if (ValidateQuerySkuDetailsResponseParams(skus, productToBuy, oldProduct))
            {
                LaunchGoogleBillingFlow(skus[0], oldProduct, desiredProrationMode);
            }
        }

        bool ValidateQuerySkuDetailsResponseParams(List<AndroidJavaObject> skus, ProductDefinition productToBuy, Product? oldProduct)
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

        static void VerifyAndWarnIfMoreThanOneSku(List<AndroidJavaObject>? skus)
        {
            if (skus?.Count > 1)
            {
                Debug.LogWarning(GoogleBillingStrings.getWarningMessageMoreThanOneSkuFound(skus[0].Call<string>("getSku")));
            }
        }

        void PurchaseFailedSkuNotFound(ProductDefinition productToBuy)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    productToBuy.id,
                    PurchaseFailureReason.ProductUnavailable,
                    "SKU does not exist in the store."
                )
            );
        }

        bool ValidateOldProduct(Product? oldProduct)
        {
            return oldProduct?.transactionID != "";
        }

        void PurchaseFailedInvalidOldProduct(ProductDefinition productToBuy, Product? oldProduct)
        {
            m_GooglePurchaseCallback.OnPurchaseFailed(
                new PurchaseFailureDescription(
                    productToBuy.id,
                    PurchaseFailureReason.ProductUnavailable,
                    "Invalid transaction id for old product: " + oldProduct?.definition.id
                )
            );
        }

        void LaunchGoogleBillingFlow(AndroidJavaObject productToPurchase, Product? oldProduct, GooglePlayProrationMode? desiredProrationMode)
        {
            var billingResult = m_BillingClient.LaunchBillingFlow(productToPurchase, oldProduct?.transactionID, desiredProrationMode);
            HandleBillingFlowResult(new GoogleBillingResult(billingResult), productToPurchase);
        }

        void HandleBillingFlowResult(IGoogleBillingResult billingResult, AndroidJavaObject sku)
        {
            if (billingResult.responseCode != GoogleBillingResponseCode.Ok)
            {
                m_GooglePurchaseCallback.OnPurchaseFailed(
                    new PurchaseFailureDescription(
                        sku.Call<string>("getSku"),
                        PurchaseFailureReason.PurchasingUnavailable,
                        billingResult.debugMessage
                    )
                );
            }
        }
    }
}

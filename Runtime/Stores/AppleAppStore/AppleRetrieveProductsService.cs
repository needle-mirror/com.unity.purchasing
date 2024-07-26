#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Security;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class AppleRetrieveProductsService : IAppleRetrieveProductsService
    {
        readonly IAppleReceiptConverter m_ReceiptConverter;

        INativeAppleStore? m_NativeStore;

        public string? LastRequestProductsJson { get; private set; }

        readonly TaskQueue queue = new TaskQueue();
        TaskCompletionSource<List<ProductDescription>>? m_CurrentRequestCompletionSource;

        [Preserve]
        internal AppleRetrieveProductsService(IAppleReceiptConverter receiptConverter)
        {
            m_ReceiptConverter = receiptConverter;
        }

        public void SetNativeStore(INativeAppleStore nativeStore)
        {
            m_NativeStore = nativeStore;
        }

        public virtual Task<List<ProductDescription>> RetrieveProducts(
            IReadOnlyCollection<ProductDefinition> products)
        {
            ValidateThatRequestIsPossible();
            return queue.Enqueue(() => ExecuteRetrieveProductsRequest(products));
        }

        void ValidateThatRequestIsPossible()
        {
            if (m_NativeStore == null)
            {
                throw new InvalidOperationException("Cannot retrieve products because the apple native store is null.");
            }
        }

        async Task<List<ProductDescription>> ExecuteRetrieveProductsRequest(IReadOnlyCollection<ProductDefinition> products)
        {
            try
            {
                m_CurrentRequestCompletionSource = new TaskCompletionSource<List<ProductDescription>>();
                m_NativeStore?.RetrieveProducts(JSONSerializer.SerializeProductDefs(products));
                return await m_CurrentRequestCompletionSource.Task;
            }
            finally
            {
                m_CurrentRequestCompletionSource = null;
            }
        }

        public void OnProductsRetrieved(string json)
        {
            LastRequestProductsJson = json;

            // get product list
            var productDescriptions = JSONSerializer.DeserializeProductDescriptions(json);

            m_CurrentRequestCompletionSource?.SetResult(productDescriptions);
        }

        ProductDescription EnrichProductDescription(ProductDescription productDescription,
            AppleReceipt appleReceipt)
        {
            // JDRjr this Find may not be sufficient for subscriptions (or even multiple non-consumables?)
            var mostRecentReceipt =
                appleReceipt.FindMostRecentReceiptForProduct(productDescription.storeSpecificId);

            if (!CanProductDescriptionBeEnriched(mostRecentReceipt))
            {
                return productDescription;
            }

            return new ProductDescription(
                productDescription.storeSpecificId,
                productDescription.metadata,
                m_NativeStore?.appReceipt,
                mostRecentReceipt?.transactionID);
        }

        bool CanProductDescriptionBeEnriched(AppleInAppPurchaseReceipt? receipt)
        {
            if (receipt == null)
            {
                return false;
            }

            var productType = (AppleStoreProductType)Enum.Parse(typeof(AppleStoreProductType),
                receipt.productType.ToString());

            return productType != AppleStoreProductType.Consumable &&
                   !IsAnExpiredAutoRenewingSubscription(receipt, productType);
        }

        bool IsAnExpiredAutoRenewingSubscription(AppleInAppPurchaseReceipt receipt, AppleStoreProductType productType)
        {
            return productType == AppleStoreProductType.AutoRenewingSubscription && IsSubscriptionExpired(receipt);
        }

        bool IsSubscriptionExpired(AppleInAppPurchaseReceipt receipt)
        {
            return new SubscriptionInfo(receipt, null).IsExpired() == Result.True;
        }

        public void OnProductDetailsRetrieveFailed(string errorMessage)
        {
            var failureDescription =
                new ProductFetchFailureDescription(ProductFetchFailureReason.Unknown,
                    $"Retrieve apple product details, failed with error message: {errorMessage}", true);
            m_CurrentRequestCompletionSource?.SetException(new RetrieveProductsException(failureDescription));
        }
    }
}

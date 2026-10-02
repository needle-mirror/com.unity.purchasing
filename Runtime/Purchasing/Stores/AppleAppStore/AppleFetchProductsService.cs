#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Purchasing.Utilities;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    [Preserve]
    class AppleFetchProductsService : IAppleFetchProductsService
    {
        INativeAppleStore? m_NativeStore;

        public string? FetchedProductsJson { get; private set; }

        readonly TaskQueue queue = new();
        TaskCompletionSource<List<ProductDescription>>? m_CurrentRequestCompletionSource;

        // Bumped by ClearFetchedProducts (auth account change). A response to a request
        // started under an older generation must not repopulate the accumulator.
        int m_ClearGeneration;
        int m_RequestGeneration;

        public void SetNativeStore(INativeAppleStore nativeStore)
        {
            m_NativeStore = nativeStore;
        }

        public virtual Task<List<ProductDescription>> FetchProducts(
            IReadOnlyCollection<ProductDefinition> products)
        {
            ValidateThatRequestIsPossible();
            return queue.Enqueue(() => ExecuteFetchProductsRequest(products));
        }

        void ValidateThatRequestIsPossible()
        {
            if (m_NativeStore == null)
            {
                throw new InvalidOperationException("Cannot retrieve products because the apple native store is null.");
            }
        }

        async Task<List<ProductDescription>> ExecuteFetchProductsRequest(IReadOnlyCollection<ProductDefinition> products)
        {
            try
            {
                m_RequestGeneration = m_ClearGeneration;
                m_CurrentRequestCompletionSource = new TaskCompletionSource<List<ProductDescription>>();
                m_NativeStore?.FetchProducts(JSONSerializer.SerializeProductDefs(products));
                return await m_CurrentRequestCompletionSource.Task;
            }
            finally
            {
                m_CurrentRequestCompletionSource = null;
            }
        }

        public void OnProductsFetched(string json)
        {
            // Stale fetch from before an auth account change — the data is account-scoped, drop it.
            // Fail here, not at the clear: native responses carry no request id.
            if (m_RequestGeneration != m_ClearGeneration)
            {
                m_CurrentRequestCompletionSource?.TrySetException(new FetchProductsException(
                    new ProductFetchFailureDescription(ProductFetchFailureReason.Unknown,
                        "The product fetch was invalidated by an auth account change.", true)));
                return;
            }

            FetchedProductsJson = JSONSerializer.MergeProductsJson(FetchedProductsJson, json);

            // get product list
            List<ProductDescription> productDescriptions;
            if (StoreKitSelector.UseStoreKit1())
            {
                productDescriptions = new AppleJsonProductDescriptionsDeserializer().DeserializeProductDescriptions(json);
            }
            else
            {
                productDescriptions= JSONSerializer.DeserializeProductDescriptionsFromFetchProductsSk2(json);
            }

            m_CurrentRequestCompletionSource?.TrySetResult(productDescriptions);
        }

        public void ClearFetchedProducts()
        {
            // An in-flight request keeps the queue occupied until its native callback
            // arrives; OnProductsFetched then discards the stale response and fails it.
            m_ClearGeneration++;
            FetchedProductsJson = null;
        }

        public void OnProductDetailsRetrieveFailed(string errorMessage)
        {
            var failureDescription =
                new ProductFetchFailureDescription(ProductFetchFailureReason.Unknown,
                    $"Retrieve apple product details, failed with error message: {errorMessage}", true);
            m_CurrentRequestCompletionSource?.TrySetException(new FetchProductsException(failureDescription));
        }
    }
}

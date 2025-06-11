#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    [Preserve]
    class AppleFetchProductsService : IAppleFetchProductsService
    {
        INativeAppleStore? m_NativeStore;

        public string? LastRequestProductsJson { get; private set; }

        readonly TaskQueue queue = new();
        TaskCompletionSource<List<ProductDescription>>? m_CurrentRequestCompletionSource;

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
            LastRequestProductsJson = json;

            // get product list
            var productDescriptions = JSONSerializer.DeserializeProductDescriptionsFromFetchProductsSk2(json);

            m_CurrentRequestCompletionSource?.SetResult(productDescriptions);
        }

        public void OnProductDetailsRetrieveFailed(string errorMessage)
        {
            var failureDescription =
                new ProductFetchFailureDescription(ProductFetchFailureReason.Unknown,
                    $"Retrieve apple product details, failed with error message: {errorMessage}", true);
            m_CurrentRequestCompletionSource?.SetException(new FetchProductsException(failureDescription));
        }
    }
}

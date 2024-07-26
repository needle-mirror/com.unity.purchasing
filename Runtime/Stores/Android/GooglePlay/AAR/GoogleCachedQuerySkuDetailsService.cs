using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    [Preserve]
    class GoogleCachedQueryProductDetailsService : IGoogleCachedQueryProductDetailsService
    {
        readonly ConcurrentDictionary<string, AndroidJavaObject> m_CachedQueriedProductDetails = new();

        ~GoogleCachedQueryProductDetailsService()
        {
            foreach (var cachedQueriedProductDetails in m_CachedQueriedProductDetails)
            {
                cachedQueriedProductDetails.Value?.Dispose();
            }
        }

        public IEnumerable<AndroidJavaObject> GetCachedQueriedProducts()
        {
            return m_CachedQueriedProductDetails.Values;
        }

        AndroidJavaObject GetCachedQueriedProductDetails(string productId)
        {
            return m_CachedQueriedProductDetails[productId];
        }

        IEnumerable<AndroidJavaObject> GetCachedQueriedProductDetails(IEnumerable<string> productIds)
        {
            return productIds.Select(GetCachedQueriedProductDetails);
        }

        public IEnumerable<AndroidJavaObject> GetCachedQueriedProductDetails(IEnumerable<ProductDefinition> products)
        {
            return GetCachedQueriedProductDetails(products.Select(product => product.storeSpecificId).ToList());
        }

        bool Contains(string productId)
        {
            return m_CachedQueriedProductDetails.ContainsKey(productId);
        }

        public bool Contains(ProductDefinition products)
        {
            return Contains(products.storeSpecificId);
        }

        public void AddCachedQueriedProductDetails(IEnumerable<AndroidJavaObject> queriedProducts)
        {
            foreach (var queriedProductDetails in queriedProducts)
            {
                var queriedProductId = queriedProductDetails.Call<string>("getProductId");
#if UNITY_2021_2_OR_NEWER
                m_CachedQueriedProductDetails[queriedProductId] = queriedProductDetails.CloneReference();
#else
                m_CachedQueriedProductDetails[queriedProductId] = queriedProductDetails;
#endif
            }
        }
    }
}

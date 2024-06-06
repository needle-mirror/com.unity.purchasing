using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Purchasing
{
    class GoogleCachedQueryProductDetailsService : IGoogleCachedQueryProductDetailsService
    {
        readonly Dictionary<string, AndroidJavaObject> m_CachedQueriedProductDetails = new Dictionary<string, AndroidJavaObject>();

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

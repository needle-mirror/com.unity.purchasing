using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Purchasing
{
    interface IGoogleCachedQueryProductDetailsService
    {
        IEnumerable<AndroidJavaObject> GetCachedQueriedProducts();
        IEnumerable<AndroidJavaObject> GetCachedQueriedProductDetails(IEnumerable<ProductDefinition> products);
        bool Contains(ProductDefinition products);
        bool ContainsSku(string sku);
        void AddCachedQueriedProductDetails(IEnumerable<AndroidJavaObject> queriedProducts);
    }
}

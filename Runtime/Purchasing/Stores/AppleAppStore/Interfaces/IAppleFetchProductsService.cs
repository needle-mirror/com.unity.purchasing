#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    interface IAppleFetchProductsService
    {
        void SetNativeStore(INativeAppleStore nativeStore);
        Task<List<ProductDescription>> FetchProducts(IReadOnlyCollection<ProductDefinition> products);
        void OnProductsFetched(string json);
        void ClearFetchedProducts();
        public void OnProductDetailsRetrieveFailed(string errorMessage);

        public string? FetchedProductsJson { get; }
    }
}

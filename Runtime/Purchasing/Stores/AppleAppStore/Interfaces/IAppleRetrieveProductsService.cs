#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    interface IAppleRetrieveProductsService
    {
        void SetNativeStore(INativeAppleStore nativeStore);
        Task<List<ProductDescription>> RetrieveProducts(IReadOnlyCollection<ProductDefinition> products);
        void OnProductsRetrieved(string json);
        public void OnProductDetailsRetrieveFailed(string errorMessage);

        public string? LastRequestProductsJson { get; }
    }
}

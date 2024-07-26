#nullable enable

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IQueryProductDetailsService
    {
        Task<List<AndroidJavaObject>> QueryProductDetails(ProductDefinition product);
        Task<List<ProductDescription>> QueryProductDescriptions(IReadOnlyCollection<ProductDefinition> products);
        Task<List<AndroidJavaObject>> QueryProductDetails(IReadOnlyCollection<ProductDefinition> products);
    }
}

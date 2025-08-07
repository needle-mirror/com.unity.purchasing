using System;
using UnityEngine.Purchasing;

namespace Samples.Purchasing.Core.CatalogProvider
{
    /// <summary>
    /// This class is the simplest representation of a product allowing us to serialize it simply.
    /// </summary>
    [Serializable]
    public class SampleCatalogProduct
    {
        public string ProductId;
        public ProductType ProductType;
        public StoreSpecificIds StoreSpecificIds;

        public SampleCatalogProduct(string id, ProductType type, StoreSpecificIds? ids = null)
        {
            ProductId = id;
            ProductType = type;
            StoreSpecificIds = ids;
        }
    }
}

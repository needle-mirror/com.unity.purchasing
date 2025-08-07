using System;
using System.Collections.Generic;

namespace Samples.Purchasing.Core.CatalogProvider
{
    /// <summary>
    /// This clas is the simplest representation of the content of a CatalogProvider allowing us to serialize it simply.
    /// </summary>
    [Serializable]
    public class SampleCatalog
    {
        public List<SampleCatalogProduct> Products;

        public SampleCatalog(List<SampleCatalogProduct> products)
        {
            Products = products;
        }

    }
}

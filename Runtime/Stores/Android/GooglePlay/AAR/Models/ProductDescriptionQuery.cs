using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing.Models
{
    class ProductDescriptionQuery
    {
        internal IReadOnlyList<ProductDefinition> products;
        internal Action<List<ProductDescription>> onProductsReceived;
        internal Action<GoogleRetrieveProductException> onRetrieveProductsFailed;

        internal ProductDescriptionQuery(IReadOnlyList<ProductDefinition> products, Action<List<ProductDescription>> onProductsReceived, Action<GoogleRetrieveProductException> onRetrieveProductsFailed)
        {
            this.products = products;
            this.onProductsReceived = onProductsReceived;
            this.onRetrieveProductsFailed = onRetrieveProductsFailed;
        }
    }
}

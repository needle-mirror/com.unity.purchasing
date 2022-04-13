using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing.Models
{
    class ProductDescriptionQuery
    {
        internal ReadOnlyCollection<ProductDefinition> products;
        internal Action<List<ProductDescription>> onProductsReceived;
        internal Action<GoogleRetrieveProductsFailureReason> onRetrieveProductsFailed;

        internal ProductDescriptionQuery(ReadOnlyCollection<ProductDefinition> products, Action<List<ProductDescription>> onProductsReceived, Action<GoogleRetrieveProductsFailureReason> onRetrieveProductsFailed)
        {
            this.products = products;
            this.onProductsReceived = onProductsReceived;
            this.onRetrieveProductsFailed = onRetrieveProductsFailed;
        }
    }
}

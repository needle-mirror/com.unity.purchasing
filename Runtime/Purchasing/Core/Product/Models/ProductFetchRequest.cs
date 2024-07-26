#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing
{
    internal class ProductFetchRequest
    {
        internal ReadOnlyCollection<ProductDefinition> RequestedProducts { get; }
        internal Action<List<Product>> SuccessAction { get; }
        internal Action<List<ProductDefinition>, string> FailureAction { get; }

        internal ProductFetchRequest(ReadOnlyCollection<ProductDefinition> products, Action<List<Product>> fetchSuccessAction,
            Action<List<ProductDefinition>, string> fetchFailureAction)
        {
            RequestedProducts = products;
            SuccessAction = fetchSuccessAction;
            FailureAction = fetchFailureAction;
        }
    }
}

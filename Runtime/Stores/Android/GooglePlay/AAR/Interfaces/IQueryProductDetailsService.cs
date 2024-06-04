using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IQueryProductDetailsService
    {
        void QueryAsyncProduct(ProductDefinition product, Action<List<AndroidJavaObject>, IGoogleBillingResult> onProductDetailsResponse);
        void QueryAsyncProduct(ReadOnlyCollection<ProductDefinition> products, Action<List<AndroidJavaObject>, IGoogleBillingResult> onProductDetailsResponse);

        void QueryAsyncProduct(ReadOnlyCollection<ProductDefinition> products, Action<List<ProductDescription>, IGoogleBillingResult> onProductDetailsResponse);
    }
}

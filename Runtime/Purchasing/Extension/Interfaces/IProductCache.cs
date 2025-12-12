#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing.Extension
{
    // Interface for a cache of products specific to the store.
    interface IProductCache : IReadOnlyProductCache
    {
        Dictionary<string, Product> productsById { get; }
        void Add(Product product);
        void Add(List<Product> product);
        void Remove(Product product);
    }
}

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing.Extension
{
    // Interface for a cache of products specific to the store.
    interface IProductCache
    {
        Dictionary<string, Product> productsById { get; }
        void Add(Product product);
        void Add(List<Product> product);
        ReadOnlyObservableCollection<Product> GetProducts();
        Product? Find(string? productId);
        Product FindOrDefault(string? productId);
        void Remove(Product product);
    }
}

#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.Extension
{
    // Interface for a cache of products specific to the store.
    public interface IProductCache
    {
        Dictionary<string, Product> productsById { get; }

        // Add products descriptions retrieved from the store. Call this after AddStoreSpecificIds.
        void Add(List<ProductDescription> productDescription);
        void Add(ProductDescription productDescription);
        void Add(IReadOnlyCollection<ProductDefinition> products);
        void Add(Product product);

        // Add store specific ids before retrieving products from the store. Call this before Add(List<ProductDescription>).
        void AddStoreSpecificIds(IReadOnlyCollection<ProductDefinition> products);
        void Remove(Product product);
        Product? Find(string? productId);
        Product FindOrDefault(string? productId);
    }
}

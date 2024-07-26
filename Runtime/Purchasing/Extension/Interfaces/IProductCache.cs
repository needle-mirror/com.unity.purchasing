#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.Extension
{
    public interface IProductCache
    {
        Dictionary<string, Product> productsById { get; }
        Dictionary<string, Product> productsByStoreSpecificId { get; }
        void Add(List<ProductDescription> productDescription);
        void Add(ProductDescription productDescription);
        void Add(IReadOnlyCollection<ProductDefinition> products);
        void Add(Product product);
        void Remove(Product product);
        Product? Find(string? productId);
        Product FindOrDefault(string? productId);
    }
}

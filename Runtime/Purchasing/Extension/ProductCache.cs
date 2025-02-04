#nullable enable

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UnityEngine.Purchasing.Extension
{
    class ProductCache : IProductCache
    {
        ObservableCollection<Product> m_Products = new();
        readonly ReadOnlyObservableCollection<Product> m_ProductsReadOnly;

        public Dictionary<string, Product> productsById { get; } = new();
        Dictionary<string, Product> productsByStoreSpecificId { get; } = new();
        Dictionary<string, string> storeSpecificProductIds { get; } = new();

        internal ProductCache()
        {
            m_ProductsReadOnly = new ReadOnlyObservableCollection<Product>(m_Products);
        }

        public void Add(List<Product> products)
        {
            foreach (var product in products)
            {
                Add(product);
            }
        }

        public void Add(Product product)
        {
            if (Find(product.definition.id) != null || Find(product.definition.storeSpecificId) != null)
            {
                return;
            }

            productsById[product.definition.id] = product;
            productsByStoreSpecificId[product.definition.storeSpecificId] = product;
            m_Products.Add(product);
        }

        public ReadOnlyObservableCollection<Product> GetProducts()
        {
            return m_ProductsReadOnly;
        }

        public void Remove(Product product)
        {
            productsById.Remove(product.definition.id);
            productsByStoreSpecificId.Remove(product.definition.storeSpecificId);
            m_Products.Remove(product);
        }

        public Product FindOrDefault(string? productId)
        {
            return Find(productId) ?? Product.CreateUnknownProduct(productId);
        }

        public Product? Find(string? productId)
        {
            return HasId(productId) ? productsById[productId!] :
                HasStoreSpecificId(productId) ? productsByStoreSpecificId[productId!] : null;
        }

        bool HasId(string? productId)
        {
            return productId != null && productsById.ContainsKey(productId);
        }

        bool HasStoreSpecificId(string? productId)
        {
            return productId != null && productsByStoreSpecificId.ContainsKey(productId);
        }
    }
}

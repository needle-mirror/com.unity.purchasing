#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing.Extension
{
    class ProductCache : IProductCache
    {
        public Dictionary<string, Product> productsById { get; } = new Dictionary<string, Product>();
        public Dictionary<string, Product> productsByStoreSpecificId { get; } = new Dictionary<string, Product>();

        public void Add(List<ProductDescription> productDescriptions)
        {
            foreach (var productDescription in productDescriptions)
            {
                Add(productDescription);
            }
        }

        public void Add(ProductDescription productDescription)
        {
            var definition = new ProductDefinition(productDescription.storeSpecificId,
                productDescription.storeSpecificId, productDescription.type);
            var product = new Product(definition, productDescription.metadata) { availableToPurchase = true };
            Add(product);
        }

        public void Add(IReadOnlyCollection<ProductDefinition> productDefinitions)
        {
            foreach (var productDefinition in productDefinitions)
            {
                Add(new Product(productDefinition, new ProductMetadata()));
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
        }

        public void Remove(Product product)
        {
            productsById.Remove(product.definition.id);
            productsByStoreSpecificId.Remove(product.definition.storeSpecificId);
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

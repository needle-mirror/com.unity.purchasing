#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    using storeSpecificIDsByProductID = Dictionary<string, string>;

    public class CatalogProvider : ICatalogProvider
    {
        Dictionary<string?, storeSpecificIDsByProductID> m_StoreSpecificIds =
            new Dictionary<string?, storeSpecificIDsByProductID>();
        List<ProductDefinition> m_Products = new List<ProductDefinition>();

        public List<ProductDefinition> GetProducts(string? storeName = null)
        {
            if (storeName != null)
            {
                UpdateStoreSpecificIDs(storeName);
            }

            return m_Products;
        }

        public void AddProduct(string id, ProductType type)
        {
            AddProduct(id, type, null);
        }

        public void AddProduct(string id, ProductType type, StoreSpecificIds? storeIDs)
        {
            AddProduct(id, type, storeIDs, (IEnumerable<PayoutDefinition>)null!);
        }

        public void AddProduct(string id, ProductType type, StoreSpecificIds? storeIDs, PayoutDefinition payout)
        {
            AddProduct(id, type, storeIDs, new List<PayoutDefinition> { payout });
        }

        public void AddProduct(string id, ProductType type, StoreSpecificIds? storeIDs, IEnumerable<PayoutDefinition>? payouts)
        {
            // Extract our store specific ID if present, according to the current store.
            var specificId = AddStoreSpecificIds(id, storeIDs);

            var product = new ProductDefinition(id, specificId, type);
            if (payouts != null)
            {
                product.SetPayouts(payouts);
            }

            m_Products.Add(product);
        }

        string AddStoreSpecificIds(string id, StoreSpecificIds? storeIDs)
        {
            if (storeIDs == null)
            {
                return id;
            }

            var lastSpecificId = id;
            using var storeSpecificIDsByStore = storeIDs.GetEnumerator();
            while (storeSpecificIDsByStore.MoveNext())
            {
                var cur = storeSpecificIDsByStore.Current;

                if (!m_StoreSpecificIds.ContainsKey(cur.Key))
                {
                    m_StoreSpecificIds.Add(cur.Key, new storeSpecificIDsByProductID());
                }

                m_StoreSpecificIds[cur.Key][id] = cur.Value;
                lastSpecificId = cur.Value;
            }

            return lastSpecificId;
        }

        public void AddProducts(IEnumerable<ProductDefinition> productsDefinitions, Dictionary<string, StoreSpecificIds>? storeIDsByProductId)
        {
            foreach (var product in productsDefinitions)
            {
                m_Products.Add(product);
                if (storeIDsByProductId != null && storeIDsByProductId.TryGetValue(product.id, out var storeSpecificIds))
                {
                    AddStoreSpecificIds(product.id, storeSpecificIds);
                }
            }
        }

        public void FetchProducts(Action<List<ProductDefinition>> callback)
        {
            FetchProducts(callback, DefaultStoreHelper.GetDefaultStoreName());
        }

        public void FetchProducts(Action<List<ProductDefinition>> callback, string? storeName)
        {
            var productDefinitions = GetProducts(storeName);
            callback(productDefinitions);
        }

        void UpdateStoreSpecificIDs(string? storeName)
        {
            foreach (var product in m_Products)
            {
                if (m_StoreSpecificIds.TryGetValue(storeName, out var storeIDs) &&
                    storeIDs.TryGetValue(product.id, out var storeSpecificId))
                {
                    product.storeSpecificId = storeSpecificId;
                }
            }
        }
    }
}

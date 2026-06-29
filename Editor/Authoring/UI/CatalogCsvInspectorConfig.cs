using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEngine;

namespace UnityEditor.Purchasing.Authoring
{
    [Serializable]
    class CatalogCsvProductEntry
    {
        public string Sku;
        public string TypeAndPrice;
    }

    [Serializable]
    class CatalogProductsList
    {
        public List<CatalogCsvProductEntry> Items = new();
    }

    [Serializable]
    class CatalogCsvInspectorConfig : ScriptableObject
    {
        public CatalogProductsList Products = new();

        public void Initialize(List<CatalogItem> items)
        {
            Products.Items.Clear();
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                var priceText = item.ProductType.ToString();
                if (item.PricingDetails is { Count: > 0 })
                {
                    var p = item.PricingDetails[0];
                    priceText = $"{item.ProductType} — {p.Amount.ToString("G", CultureInfo.InvariantCulture)} {p.CurrencyCode}";
                }

                Products.Items.Add(new CatalogCsvProductEntry
                {
                    Sku = item.uSku,
                    TypeAndPrice = priceText,
                });
            }
        }
    }
}

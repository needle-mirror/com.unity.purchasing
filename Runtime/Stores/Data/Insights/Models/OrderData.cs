#nullable enable

namespace UnityEngine.Purchasing.Stores.Data.Insights.Models
{
    // Mirrors insights.producers.iapsdk.v1alpha1.order_data.proto

    internal enum Store
    {
        Unspecified = 0,
        AppStore = 1,
        GooglePlay = 2,
        PaymentProvider = 3,
        Webshop = 4
    }

    internal enum ProductType
    {
        Unspecified = 0,
        Consumable = 1,
        NonConsumable = 2,
        Subscription = 3,
        Unknown = 4
    }

    internal sealed class Sku
    {
        public string SkuId { get; set; } = "";
        public ProductType ProductType { get; set; }
        public string? LocalizedTitle { get; set; }
        public string? LocalizedDescription { get; set; }
        public string? LocalizedPriceString { get; set; }
        public long? PriceMicro { get; set; }
        public string? IsoCurrencyCode { get; set; }
        public long Quantity { get; set; } = 1;
    }

    internal sealed class OrderData
    {
        public Sku Sku { get; set; } = null!;
        public string? StoreTransactionId { get; set; }
    }
}

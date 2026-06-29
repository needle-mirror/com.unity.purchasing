using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Purchasing.PaymentProviders;
using UnityEngine.Purchasing.PaymentProviderService.Models;

namespace UnityEngine.Purchasing.PaymentProviderService
{
    /// <summary> Interface for PaymentProviderService </summary>
    internal interface IPaymentProviderService
    {
        public Task<OrderData> GetUrl(string catalogListingId, string displayName, string locale, string currencyCode,
            string country, PlayerIdentity playerIdentity, string paymentProviderOverride, DeviceInfo deviceInfo, IReadOnlyList<PaymentProviderToken> paymentProviderTokens = null);
        public Task<List<ProductData>> GetProducts(List<string> skus, string locale, string currencyCode);
        public Task<List<CatalogProductData>> GetCatalog(List<string> stores);
        public Task<List<OrderData>> GetEntitledOrders();
        public Task<OrderData> GetOrder(string orderId);
        public Task<OrderData> UpdateOrder(string orderId, UpdateOrderStatus status);
        // Returns a (providers, paymentOptionPopupEnabled) tuple instead of the public
        // EligiblePaymentProviders type because this assembly is a dependency of Unity.Purchasing
        // where that type lives. The bool is the resolved value with the wire-default already applied.
        public Task<(List<string> Providers, bool PaymentOptionPopupEnabled)> GetEligiblePaymentProviders();
    }

}

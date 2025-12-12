#nullable enable
namespace UnityEngine.Purchasing
{
    class UnknownPurchasedProductInfo : IPurchasedProductInfo
    {
        public string productId => "";
        public SubscriptionInfo? subscriptionInfo => null;
    }
}

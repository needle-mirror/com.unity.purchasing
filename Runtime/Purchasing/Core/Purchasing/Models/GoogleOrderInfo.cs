#nullable enable

using UnityEngine.Purchasing.Security;

namespace UnityEngine.Purchasing
{
    class GoogleOrderInfo : OrderInfo, IGoogleOrderInfo
    {
        public string? ObfuscatedAccountId { get; set; }
        public string? ObfuscatedProfileId { get; set; }
        public string? OrderId { get; }
        public string PurchaseToken { get; }

        // The purchase token is used as the transaction id on Google Play, hence the base call.
        public GoogleOrderInfo(string receipt, string? purchaseToken, string storeName, string? obfuscatedAccountId, string? obfuscatedProfileId, string? orderId)
            : base(receipt, purchaseToken, storeName)
        {
            ObfuscatedAccountId = obfuscatedAccountId;
            ObfuscatedProfileId = obfuscatedProfileId;
            OrderId = orderId;
            PurchaseToken = purchaseToken ?? "";
        }
    }
}

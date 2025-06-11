#nullable enable

using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePurchase
    {
        int purchaseState { get; }
        List<string> skus { get; }
        string orderId { get; }
        string receipt { get; }
        string signature { get; }
        string? obfuscatedAccountId { get; }
        string? obfuscatedProfileId { get; }
        string originalJson { get; }
        string purchaseToken { get; }
        public IEnumerable<ProductDescription> productDescriptions { get; }
        string? sku { get; }
        bool IsAcknowledged();
        bool IsPurchased();
        bool IsPending();
    }
}

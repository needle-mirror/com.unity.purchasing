#nullable enable

using System.Collections.Generic;
using UnityEngine.Purchasing.Utils;

namespace UnityEngine.Purchasing.Interfaces
{
    internal interface IGooglePurchase
    {
        IAndroidJavaObjectWrapper javaPurchase { get; }
        int purchaseState { get; }
        List<string> skus { get; }
        string orderId { get; }
        string receipt { get; }
        string signature { get; }
        string originalJson { get; }
        string purchaseToken { get; }
        string? sku { get; }

        bool IsAcknowledged();

        bool IsPurchased();
    }
}

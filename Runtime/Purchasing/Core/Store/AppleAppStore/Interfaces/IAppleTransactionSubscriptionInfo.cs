#nullable enable
using System;

namespace UnityEngine.Purchasing
{
    internal interface IAppleTransactionSubscriptionInfo
    {
        OfferType OfferType { get; }
        string? OfferId { get; }
        bool? IsFree { get; }
        DateTime? ExpirationDate { get; }
        DateTime? RevocationDate { get; }
        DateTime? PurchaseDate { get; }
        AppleStoreProductType ProductType { get; }
        string? RenewalProductId { get; }
    }
}

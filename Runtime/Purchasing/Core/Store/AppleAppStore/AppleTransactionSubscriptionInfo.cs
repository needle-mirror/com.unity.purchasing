#nullable enable
using System;

namespace UnityEngine.Purchasing
{
    class AppleTransactionSubscriptionInfo : IAppleTransactionSubscriptionInfo
    {
        public OfferType OfferType { get; }
        public string? OfferId { get; }
        public bool? IsFree { get; }
        public DateTime? ExpirationDate { get; }
        public DateTime? RevocationDate { get; }
        public DateTime? PurchaseDate { get; }
        public AppleStoreProductType ProductType { get; }
        public string? RenewalProductId { get; }

        internal AppleTransactionSubscriptionInfo(OfferType offerType, string? offerId, bool? isFree, DateTime? expirationDate, DateTime? revocationDate, DateTime? purchaseDate, AppleStoreProductType productType, string renewalProductId)
        {
            OfferType = offerType;
            OfferId = offerId;
            IsFree = isFree;
            ExpirationDate = expirationDate;
            RevocationDate = revocationDate;
            PurchaseDate = purchaseDate;
            ProductType = productType;
            RenewalProductId = renewalProductId;
        }
    }
}

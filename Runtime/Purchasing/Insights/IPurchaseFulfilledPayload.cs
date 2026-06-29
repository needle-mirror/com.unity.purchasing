#nullable enable

namespace UnityEngine.Purchasing
{
    // Carries the store-specific data attached to a PurchaseFulfilledEvent.
    // Lives in Unity.Purchasing so that IPurchaseEventEmitter and the Apple/Google
    // extended services can reference a common type; the concrete
    // PurchaseEventEmitter in Unity.Purchasing.Stores translates it to the
    // proto-faithful Insights AppStorePayload / GooglePlayPayload at write
    // time.
    internal interface IPurchaseFulfilledPayload { }

    internal sealed class ApplePurchaseFulfilledPayload : IPurchaseFulfilledPayload
    {
        public string? JwsRepresentation { get; set; }
        public string? OriginalTransactionId { get; set; }
        public OwnershipType Ownership { get; set; }
        public string? AppReceipt { get; set; }
        public string? AppAccountToken { get; set; }
    }

    internal sealed class GooglePurchaseFulfilledPayload : IPurchaseFulfilledPayload
    {
        public string OriginalJson { get; set; } = "";
        public string Signature { get; set; } = "";
    }
}

#nullable enable

namespace UnityEngine.Purchasing.Stores.Data.Insights.Models
{
    // Mirrors insights.producers.iapsdk.v1alpha1.store_payload.proto

    internal interface IStorePayload { }

    internal enum OwnershipType
    {
        Unspecified = 0,
        Purchased = 1,
        FamilyShared = 2
    }

    internal sealed class AppStorePayload : IStorePayload
    {
        public string? AppReceipt { get; set; }
        public string? JwsRepresentation { get; set; }
        public string? OriginalTransactionId { get; set; }
        public string? AppAccountToken { get; set; }
        public OwnershipType OwnershipType { get; set; }
    }

    internal sealed class GooglePlayPayload : IStorePayload
    {
        public string OriginalJson { get; set; } = "";
        public string Signature { get; set; } = "";
    }
}

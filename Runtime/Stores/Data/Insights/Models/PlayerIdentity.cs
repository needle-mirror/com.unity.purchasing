#nullable enable

namespace UnityEngine.Purchasing.Stores.Data.Insights.Models
{
    // Mirrors insights.common.v1alpha1.PlayerIdentity
    // (see Schemas~/insights/common/v1alpha1/player_identity.proto).
    // All fields are optional because not every identity type is available
    // in every context.
    internal sealed class PlayerIdentity
    {
        public string? UnityInstallationId { get; set; }
        public string? PlayerId { get; set; }
        public string? UserId { get; set; }
        public string? AnalyticsId { get; set; }
        public string? Idfa { get; set; }
        public string? Gaid { get; set; }
        public string? Idfv { get; set; }
        public string? UnityAdsIdfi { get; set; }
        public string? AppInstanceId { get; set; }
    }
}

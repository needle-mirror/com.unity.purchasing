#nullable enable

using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// One catalog listing's result from <see cref="IPaymentProvidersExtendedService.GetPurchaseEligibility"/>.
    /// </summary>
    public class PurchaseEligibility
    {
        /// <summary>
        /// The catalog listing id that was checked.
        /// </summary>
        public string CatalogListingId { get; }

        /// <summary>
        /// Whether the player can buy this listing now, given its purchase limit.
        /// False when the listing could not be checked; see <see cref="FailureReason"/>.
        /// </summary>
        public bool Eligible { get; }

        /// <summary>
        /// Purchases left in the current period. Null when the listing has no purchase limit,
        /// or when its product has not been fetched.
        /// </summary>
        public int? Remaining { get; }

        /// <summary>
        /// Start of the next period of the listing's purchase limit, in UTC. Set only when <see cref="Eligible"/> is false.
        /// </summary>
        public DateTime? NextEligibleAt { get; }

        /// <summary>
        /// Why the listing could not be checked. Null when the check succeeded.
        /// Do not treat a listing with a failure reason as eligible.
        /// </summary>
        public PurchaseEligibilityFailureReason? FailureReason { get; }

        /// <summary>
        /// The cause of the failure, for example the service error or the failure reason name.
        /// Null when the check succeeded.
        /// </summary>
        public string? FailureMessage { get; }

        internal PurchaseEligibility(string catalogListingId, bool eligible, int? remaining, DateTime? nextEligibleAt, PurchaseEligibilityFailureReason? failureReason = null, string? failureMessage = null)
        {
            CatalogListingId = catalogListingId;
            Eligible = eligible;
            Remaining = remaining;
            NextEligibleAt = nextEligibleAt;
            FailureReason = failureReason;
            FailureMessage = failureMessage;
        }
    }
}

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Why <see cref="IPaymentProvidersExtendedService.GetPurchaseEligibility"/> could not check a catalog listing.
    /// </summary>
    public enum PurchaseEligibilityFailureReason
    {
        /// <summary>The request failed, for example a network, authentication, or service error.</summary>
        RequestFailed,

        /// <summary>The catalog could not be read.</summary>
        CatalogUnavailable,

        /// <summary>The catalog has no listing with this id.</summary>
        ListingNotFound,

        /// <summary>The listing's purchase limit is not valid.</summary>
        InvalidPurchaseLimit
    }
}

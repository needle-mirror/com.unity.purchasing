using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Reasons for which purchasing initialization could fail.
    /// </summary>
    [Obsolete("Please upgrade to the new APIs available. For more info visit `Upgrading to IAP v5` in the IAP documentation. https://docs.unity3d.com/Packages/com.unity.purchasing@latest", false)]
    public enum InitializationFailureReason
    {
        /// <summary>
        /// In App Purchases disabled in device settings.
        /// </summary>
        PurchasingUnavailable,

        /// <summary>
        /// No products available for purchase,
        /// Typically indicates a configuration error.
        /// </summary>
        NoProductsAvailable,

        /// <summary>
        /// The store reported the app as unknown.
        /// Typically indicates the app has not been created
        /// on the relevant developer portal, or the wrong
        /// identifier has been configured.
        /// </summary>
        AppNotKnown
    }
}

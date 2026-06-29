namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// Internal storage for shared [Obsolete] attribute messages.
    /// Extracted from <see cref="UnityEngine.Purchasing.Extension.UnityUtil"/> so referencing these strings in [Obsolete(...)] attributes
    /// across the package doesn't introduce a bare-identifier reference to "UnityUtil" in source —
    /// which can shadow against a same-named top-level namespace in any auto-referenced precompiled
    /// DLL a consumer brings in (ULO-10387). The IAP-prefixed class name further reduces the chance
    /// of collision with a consumer's own type of the same name.
    /// </summary>
    internal static class IAPObsoleteMessages
    {
        internal const string UpgradeToIAPV5 =
            "This API is deprecated. Please upgrade to the new APIs introduced in IAP v5. For more information, visit the IAP manual: https://docs.unity.com/ugs/en-us/manual/iap/manual/upgrade-to-iap-v5";
    }
}

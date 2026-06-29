namespace UnityEngine.Purchasing
{
    // Single source of truth for the com.unity.purchasing version string.
    // Surfaced to apps via StandardPurchasingModule.Version, sent on the
    // Unity-IAP-Package-Version request header by InternalPaymentProviderService,
    // emitted in IAPSDKEvent envelopes, and reported in the
    // X-SDK-Release-Version Insights-ingest header. See CLAUDE.md
    // "Version Bump (Soft Files)" — update here, not at the call sites.
    internal static class IAPVersion
    {
        public const string Current = "5.4.0";
    }
}

namespace UnityEngine.Purchasing.Stores.Data.Insights.Models
{
    // Mirrors insights.common.v1alpha1.producer_event_type_enum.proto.
    //
    // Identifies the SDK that produced a payload pushed through the Insights
    // Module's LogEvent() transport. Values are wire-compatible with the
    // proto enum and must not be renumbered.
    internal enum EventType
    {
        Unspecified = 0,
        IapSdk = 1000
    }
}

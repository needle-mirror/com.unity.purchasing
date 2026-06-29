namespace UnityEngine.Purchasing.Stores.Data.Insights.Models
{
    // Mirrors insights.common.v1alpha1.consent.proto

    internal enum ConsentState
    {
        Unspecified = 0,
        Unset = 1,
        Granted = 2,
        Denied = 3
    }

    internal enum DataCollectionMode
    {
        Unspecified = 0,
        Basic = 1,
        Recommended = 2
    }
}

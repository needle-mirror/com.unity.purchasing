
using System;

namespace UnityEngine.Purchasing
{
    public enum EventDestType
    {
        Unknown,
        AdsTracking,    // simple GET using Ads TrackingUrl
        IAP,            // POST to iap-events
        Analytics,      // if using custom or standard events
        CDP,            // official 2018 API for new event types
        CDPDirect,      // use this for direct connect CDP
        AdsIPC          // IPC via Ads SendEvent
    }
}

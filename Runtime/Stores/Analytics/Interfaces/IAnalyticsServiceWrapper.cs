#if IAP_ANALYTICS_SERVICE_ENABLED || IAP_ANALYTICS_SERVICE_ENABLED_WITH_SERVICE_COMPONENT
#nullable enable

using Unity.Services.Analytics;

namespace UnityEngine.Purchasing
{
    interface IAnalyticsServiceWrapper
    {
        IAnalyticsService? AnalyticsServiceInstance();
    }
}

#endif

#if IAP_ANALYTICS_SERVICE_ENABLED || IAP_ANALYTICS_SERVICE_ENABLED_WITH_SERVICE_COMPONENT
#nullable enable

using Unity.Services.Analytics;
using Unity.Services.Core;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    [Preserve]
    class AnalyticsServiceWrapper : IAnalyticsServiceWrapper
    {
        public IAnalyticsService? AnalyticsServiceInstance()
        {
            try
            {
                return AnalyticsService.Instance;
            }
            catch (ServicesInitializationException ex)
            {
                Debug.Log("Unity Purchasing: Failed to initialize Unity Analytics. " + ex.Message + " Disabling analytics.");
                return null;
            }
        }
    }
}

#endif

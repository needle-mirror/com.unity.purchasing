// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.
#if UNITY_2023_2_OR_NEWER
using UnityEngine.Analytics;

namespace Unity.Purchasing.Editor.Shared.Analytics
{
    class CommonAnalyticProvider : ICommonAnalyticProvider
    {
        public IAnalytic GetAnalytic(ICommonAnalytics.CommonEventPayload payload)
        {
            return new CommonAnalyticEvent(payload);
        }
    }
}
#endif

// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using UnityEditor;
using UnityEngine.Analytics;

namespace Unity.Purchasing.Editor.Shared.Analytics
{
    class CommonAnalytics : ICommonAnalytics
    {
        // cloud code vendor key is for legacy reasons.
        // We would need a new event for a new vendor key
        public const string vendorKey = "unity.services.cloudcode.authoring";
        public const string eventName = "shared_common";
        public const int version = 1;

#if UNITY_2023_2_OR_NEWER
        ICommonAnalyticProvider m_AnalyticProvider;

        public CommonAnalytics(ICommonAnalyticProvider analyticProvider)
        {
            m_AnalyticProvider = analyticProvider;
        }

#endif

        public AnalyticsResult Send(ICommonAnalytics.CommonEventPayload payload)
        {
#if UNITY_2023_2_OR_NEWER
            return EditorAnalytics.SendAnalytic(m_AnalyticProvider.GetAnalytic(payload));
#else
            return EditorAnalytics.SendEventWithLimit(eventName, payload, version);
#endif
        }
    }
}

// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;
using UnityEngine.Analytics;

namespace Unity.Purchasing.Editor.Shared.Analytics
{
    interface ICommonAnalytics
    {
        public AnalyticsResult Send(CommonEventPayload payload);

        [Serializable]

        // Naming exception to the standard in order to match the schema
        // ReSharper disable InconsistentNaming
        public struct CommonEventPayload
#if UNITY_2023_2_OR_NEWER
            : IAnalytic.IData
#endif
        {
            public string action;
            public long duration;
            public int count;
            public string context;
            public string environment;
            public string exception;

            // old analytics automatically appended these items, and as such we added them to the schema
            // new analytics does not append these, so we must include them in #ifdef
#if UNITY_2023_2_OR_NEWER
            public string package;
            public string package_ver;
#endif
        }

        // ReSharper restore InconsistentNaming
    }
}

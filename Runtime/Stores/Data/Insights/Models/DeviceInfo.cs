#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing.Stores.Data.Insights.Models
{
    // Mirrors insights.common.v1alpha1.device_info.proto
    //
    // Distinct from UnityEngine.Purchasing.Stores.DeviceInfo (the SDK's
    // device-info type, which also carries Platform/AppBundleID). Those two
    // fields belong on the proto Reporting message, not here.
    internal sealed class DeviceInfo
    {
        public string SystemLanguage { get; set; } = "";
        public List<string> LocaleList { get; set; } = new();
        public string Model { get; set; } = "";
        public string SystemBootTime { get; set; } = "";
        public string OsVersion { get; set; } = "";
        public ulong TotalSpace { get; set; }
    }
}

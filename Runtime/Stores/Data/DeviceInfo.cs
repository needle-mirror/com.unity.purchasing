#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing.Stores
{
    class DeviceInfo
    {
        public string? Language { get; set; }
        public string? Platform { get; set; }
        public List<string>? LocaleList { get; set; }
        public string? DeviceModel { get; set; }
        public long? SystemBootTime { get; set; }
        public string? OSVersion { get; set; }
        public string? AppBundleID { get; set; }
        public long? TotalSpace { get; set; }
    }
}

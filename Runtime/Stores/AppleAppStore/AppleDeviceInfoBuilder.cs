#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.MiniJSON;

namespace UnityEngine.Purchasing.Stores
{
    static class AppleDeviceInfoBuilder
    {
        internal static DeviceInfo Build(INativeAppleStore? nativeStore)
        {

            var nativeJson = nativeStore?.FetchNativeDeviceInfo() ?? "{}";
            var nativeDict = Json.Deserialize(nativeJson) as Dictionary<string, object>
                             ?? new Dictionary<string, object>();

            var osVersion = nativeDict.TryGetValue("osVersion", out var osVersionRaw)
                ? osVersionRaw as string
                : null;

            var localeListRaw = nativeDict.TryGetValue("localeList", out var ll)
                ? ll as List<object>
                : null;
            var localeList = localeListRaw?.Select(l => l?.ToString().Replace("-", "_") ?? "").ToList()
                             ?? new List<string>();

            var languageRaw = nativeDict.TryGetValue("language", out var lang)
                ? lang as string
                : null;
            var language = languageRaw ?? localeList.FirstOrDefault();

            var deviceModel = nativeDict.TryGetValue("deviceModel", out var dev)
                ? dev as string
                : null;

            long? systemBootTime;
            try
            {
                 systemBootTime = nativeDict.TryGetValue("systemBootTime", out var sbt) && sbt != null
                    ? Convert.ToInt64(sbt)
                    : null;
            }
            catch
            {
                systemBootTime = null;
            }

            long? totalSpace;
            try
            {
                totalSpace = nativeDict.TryGetValue("totalSpace", out var ts) && ts != null
                    ? Convert.ToInt64(ts)
                    : null;
            }
            catch
            {
                totalSpace = null;
            }

            return new DeviceInfo
            {
                Language = language,
                Platform = "ios",
                LocaleList = localeList,
                DeviceModel = deviceModel,
                SystemBootTime = systemBootTime,
                OSVersion = osVersion,
                AppBundleID = Application.identifier,
                TotalSpace = totalSpace
            };
        }
    }
}

#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing.Stores
{
    static class AndroidDeviceInfoBuilder
    {
        internal static DeviceInfo Build()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var localeClass = new AndroidJavaClass("java.util.Locale");
            using var defaultLocale = localeClass.CallStatic<AndroidJavaObject>("getDefault");
            var language = defaultLocale.Call<string>("toString") ?? "";

            using var buildClass = new AndroidJavaClass("android.os.Build");
            var deviceModel = buildClass.GetStatic<string>("MODEL") ?? "";

            using var versionClass = new AndroidJavaClass("android.os.Build$VERSION");
            var osVersion = versionClass.GetStatic<string>("RELEASE") ?? "";
            var sdkInt = versionClass.GetStatic<int>("SDK_INT");

            using var activity = UnityActivity.GetCurrentActivity();
            var appBundleId = activity.Call<string>("getPackageName") ?? "";

            using var systemClass = new AndroidJavaClass("java.lang.System");
            var currentTimeMs = systemClass.CallStatic<long>("currentTimeMillis");
            using var clockClass = new AndroidJavaClass("android.os.SystemClock");
            var elapsedMs = clockClass.CallStatic<long>("elapsedRealtime");
            var systemBootTime = (currentTimeMs - elapsedMs) / 1000L;

            long totalSpaceKB = 0;
            using var externalCacheDir = activity.Call<AndroidJavaObject>("getExternalCacheDir");
            if (externalCacheDir != null)
            {
                totalSpaceKB = externalCacheDir.Call<long>("getTotalSpace") / 1024L;
            }

            var localeList = new List<string>();
            if (sdkInt >= 24)
            {
                using var localeListClass = new AndroidJavaClass("android.os.LocaleList");
                using var localeListObj = localeListClass.CallStatic<AndroidJavaObject>("getDefault");
                var count = localeListObj.Call<int>("size");
                for (int i = 0; i < count; i++)
                {
                    using var loc = localeListObj.Call<AndroidJavaObject>("get", i);
                    localeList.Add(loc.Call<string>("toString") ?? "");
                }
            }
            else
            {
                var availableLocales = localeClass.CallStatic<AndroidJavaObject[]>("getAvailableLocales");
                if (availableLocales != null)
                {
                    foreach (var loc in availableLocales)
                    {
                        localeList.Add(loc?.Call<string>("toString") ?? "");
                        loc?.Dispose();
                    }
                }
            }

            return new DeviceInfo
            {
                Language = language,
                Platform = "android",
                LocaleList = localeList,
                DeviceModel = deviceModel,
                SystemBootTime = systemBootTime,
                OSVersion = osVersion,
                AppBundleID = appBundleId,
                TotalSpace = totalSpaceKB
            };
#else
            return new DeviceInfo { Platform = "android" };
#endif
        }
    }
}

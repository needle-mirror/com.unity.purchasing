#nullable enable

using System;
using System.IO;
using System.Threading;

namespace UnityEngine.Purchasing.Stores
{
    // Fetches the app installation timestamp from the platform.
    //
    // This is the IAP SDK's fallback for ULO-9776. The engine Insights
    // module (available 2022.3+) does not auto-populate
    // installation_timestamp on the envelope until the engine PR ULO-10238
    // (unity/unity #105897) lands. On older Unity versions — where
    // Insights is present but doesn't fill this field — the SDK populates
    // it itself, mirroring what the engine does in newer versions:
    //   - Android: PackageInfo.firstInstallTime via PackageManager (JNI)
    //   - iOS:     persistentDataPath creation date (created at first
    //              launch on install). Application.dataPath cannot be used
    //              because Apple normalizes .app bundle file metadata on
    //              signing/install, returning Unix epoch for creation time.
    //              Verified on-device 2026-06-11.
    //   - Other:   null (no reliable cross-platform source)
    static class AppInstallInfo
    {
        // Install timestamp is immutable for the lifetime of the install, so
        // cache it after the first lookup to avoid repeating JNI calls (Android)
        // or disk I/O (iOS) on every Insights event emission. Lazy<T> with
        // ExecutionAndPublication mode provides formal thread-safety guarantees
        // even though PurchaseEventEmitter's constructor pre-warms the cache
        // on the main thread (see PurchaseEventEmitter.cs constructor).
        static Lazy<DateTime?> s_LazyTimestamp = new(FetchInstallTimestamp, LazyThreadSafetyMode.ExecutionAndPublication);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnLoad()
        {
            s_LazyTimestamp = new Lazy<DateTime?>(FetchInstallTimestamp, LazyThreadSafetyMode.ExecutionAndPublication);
        }

        internal static DateTime? GetInstallTimestamp() => s_LazyTimestamp.Value;

        static DateTime? FetchInstallTimestamp()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var activity = UnityActivity.GetCurrentActivity();
                using var packageManager = activity.Call<AndroidJavaObject>("getPackageManager");
                var packageName = activity.Call<string>("getPackageName");
                using var packageInfo = packageManager.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);
                var firstInstallTimeMs = packageInfo.Get<long>("firstInstallTime");
                return DateTimeOffset.FromUnixTimeMilliseconds(firstInstallTimeMs).UtcDateTime;
            }
            catch
            {
                return null;
            }
#elif UNITY_IOS && !UNITY_EDITOR
            try
            {
                // persistentDataPath is a directory, use Directory.GetCreationTimeUtc
                // for semantic precision (File.* also works but is for files).
                var creationTime = Directory.GetCreationTimeUtc(Application.persistentDataPath);
                // .NET returns 1601-01-01T00:00:00Z (NOT DateTime.MinValue which is
                // 0001-01-01) when the path does not exist or is inaccessible.
                return creationTime.Year > 1601 ? (DateTime?)creationTime : null;
            }
            catch
            {
                return null;
            }
#else
            return null;
#endif
        }
    }
}

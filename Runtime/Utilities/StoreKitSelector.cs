using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Purchasing;

namespace Purchasing.Utilities
{
    public static class StoreKitSelector
    {
        /// <summary>
        /// A flag used to force StoreKit1.
        /// This flag must be set to true before initializing Unity IAP.
        ///
        /// By default, iOS, iPadOS and tvOS below 15.0 will be using StoreKit1.
        ///
        /// This flag is meant to help test StoreKit1.
        /// We don't recommend changing this setting in production.
        /// </summary>
        public static bool forceStoreKit1 = false;

        /// <summary>
        /// Determine whether to use StoreKit1 on the current device.
        /// This is only evaluated once.
        /// </summary>
        public static bool UseStoreKit1()
        {
            if (!s_StoreKitSelected)
            {
                s_UseStoreKit1 = ComputeUseStoreKit1();
                Debug.unityLogger.Log(s_UseStoreKit1 ? "StoreKitSelector: Using StoreKit 1" : "StoreKitSelector: Using StoreKit 2");
                s_StoreKitSelected = true;
            }

            return s_UseStoreKit1;
        }

        static bool s_StoreKitSelected = false;
        static bool s_UseStoreKit1 = ComputeUseStoreKit1();

        static bool ComputeUseStoreKit1()
        {
            if (forceStoreKit1)
            {
                return true;
            }

#if UNITY_IOS || UNITY_TVOS
            var ver = UnityEngine.iOS.Device.systemVersion; // e.g., "16.6" or "14.8.1"
            if (string.IsNullOrEmpty(ver))
                return false;

            var dot = ver.IndexOf('.');
            var majorStr = dot >= 0 ? ver.Substring(0, dot) : ver;
            var t = int.TryParse(majorStr, out var major) && major < 15;
            return t;
#elif UNITY_VISIONOS
            return true;
#else
            var major = ExtractFirstInteger(SystemInfo.operatingSystem);
            if (major >= 0)
                return major < 12;

            // Fallback: map Darwin kernel major to macOS major
            // Darwin 22 => macOS 13, 21 => 12, 20 => 11, <=19 => 10.x
            try
            {
                var darwinMajor = Environment.OSVersion.Version.Major;
                return darwinMajor < 21;
            }
            catch
            {
                return false;
            }
#endif
        }

        static int ExtractFirstInteger(string s)
        {
            if (string.IsNullOrEmpty(s)) return -1;
            var i = 0;
            while (i < s.Length && !char.IsDigit(s[i])) i++;
            if (i == s.Length) return -1;
            var start = i;
            while (i < s.Length && char.IsDigit(s[i])) i++;
            return int.TryParse(s.Substring(start, i - start), out int n) ? n : -1;
        }
    }
}

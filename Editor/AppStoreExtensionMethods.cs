using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    internal static class AppStoreExtensionMethods
    {
        static readonly Dictionary<AppStore, string> AppStoreDisplayNames = new Dictionary<AppStore, string>()
        {
            {AppStore.AppleAppStore, AppleAppStore.DisplayName},
            {AppStore.GooglePlay, GooglePlay.DisplayName},
            {AppStore.MacAppStore, MacAppStore.DisplayName},
            {AppStore.fake, FakeAppStore.DisplayName}
        };

        public static string ToDisplayName(this AppStore value)
        {
            return AppStoreDisplayNames.ContainsKey(value) ? AppStoreDisplayNames[value] : "";
        }

        public static AppStore ToAppStoreFromDisplayName(this string value)
        {
            if (AppStoreDisplayNames.ContainsValue(value))
            {
                var dict = AppStoreDisplayNames;
                return dict.FirstOrDefault(x => x.Value == value).Key;
            }

            return AppStore.NotSpecified;
        }

        public static bool IsAndroid(this AppStore value)
        {
            return (int)value >= (int)AppStoreMeta.AndroidStoreStart &&
                   (int)value <= (int)AppStoreMeta.AndroidStoreEnd;
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing.UI.Presenters
{
    class PlatformsStoreSettingsPresenter
    {
        static readonly List<string> k_StoreNames = new List<string>
        {
            "Amazon Appstore",
            "Google Play",
            "Apple App Store",
            "Mac App Store",
            "Unity Distribution Portal",
            "Microsoft Store"
        };

        internal int GetIndexOfAndroidStore(AppStore appStore)
        {
            if (appStore.IsAndroid())
            {
                return BuildTargetGroup.Android.ToAppStores().IndexOf(appStore);
            }
            else
            {
                return -1;
            }
        }

        internal string[] GetCurrentStoreTargetContainerOptions()
        {
            return EditorUserBuildSettings.activeBuildTargetGroup.ToAppStoreDisplayNames().ToArray();
        }

        internal int GetCurrentStoreTargetContainerIndex()
        {
            return EditorUserBuildSettings.activeBuildTargetGroup.ToAppStoreDisplayNames().IndexOf(GetCurrentStoreTarget());
        }

        static string GetCurrentStoreTarget()
        {
            var currentStoreTargets = EditorUserBuildSettings.activeBuildTargetGroup.ToAppStoreDisplayNames();

            return currentStoreTargets.Count == 1 ? currentStoreTargets.First() : UnityPurchasingEditor.ConfiguredAppStore().ToDisplayName();
        }

        internal IEnumerable<string> GetSupportedStores()
        {
            return GetSupportedStoresIncludingTarget();
        }

        internal static IEnumerable<string> GetSupportedStoresIncludingTarget()
        {
            return new List<string>(EditorUserBuildSettings.activeBuildTargetGroup.ToAppStoreDisplayNamesExcludingDefault());
        }

        internal IEnumerable<string> GetOtherStores()
        {
            var supportedStores = GetSupportedStoresIncludingTarget();
            var otherStores = GetAllStores().ToList();
            otherStores.RemoveAll(store => supportedStores.Contains(store));

            return otherStores;
        }

        internal IEnumerable<string> GetAllStores()
        {
            return k_StoreNames;
        }
    }
}

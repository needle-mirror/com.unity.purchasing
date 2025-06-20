#nullable enable

namespace UnityEngine.Purchasing
{
    public static class DefaultStoreHelper
    {
        static string s_DefaultCustomStoreOverrideName = string.Empty;

        public static void OverrideDefaultStoreName(string newDefaultStoreName)
        {
            s_DefaultCustomStoreOverrideName = newDefaultStoreName;
        }

        public static string GetDefaultStoreName()
        {
            return string.IsNullOrEmpty(s_DefaultCustomStoreOverrideName) ? GetBuiltInDefaultStoreName() : s_DefaultCustomStoreOverrideName;
        }

        static string GetBuiltInDefaultStoreName()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXPlayer:
                    return MacAppStore.Name;
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.tvOS:
#if UNITY_VISIONOS
                case RuntimePlatform.VisionOS:
#endif
                    return AppleAppStore.Name;
                case RuntimePlatform.Android:
                    return SelectedAndroidStoreHelper.GetSelectedAndroidStoreName();
                default:
                    return FakeAppStore.Name;
            }
        }

        internal static AppStore GetDefaultBuiltInAppStore()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXPlayer:
                    return AppStore.MacAppStore;
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.tvOS:
#if UNITY_VISIONOS
                case RuntimePlatform.VisionOS:
#endif
                    return AppStore.AppleAppStore;
                case RuntimePlatform.Android:
                    return SelectedAndroidStoreHelper.GetSelectedAndroidStore();
                default:
                    return AppStore.fake;
            }
        }
    }
}

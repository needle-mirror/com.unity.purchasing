using UnityEngine;
using UnityEngine.Purchasing;

namespace UnityEngine.Purchasing
{
    static class SelectedAndroidStoreHelper
    {
        static readonly string k_BillingModeFileName = "BillingMode";

        internal static string GetSelectedAndroidStoreName()
        {
            var androidStore = GetSelectedAndroidStore();
            switch (androidStore)
            {
                case AppStore.GooglePlay:
                    return GooglePlay.Name;
                default:
                    return FakeAppStore.Name;
            }
        }

        internal static AppStore GetSelectedAndroidStore()
        {
            var config = LoadStoreConfiguration();
            var androidStore = (config != null) ? config.androidStore : AppStore.NotSpecified;

            return androidStore;
        }

        static StoreConfiguration LoadStoreConfiguration()
        {
            var textAsset = Resources.Load(k_BillingModeFileName) as TextAsset;
            StoreConfiguration config = null;
            if (null != textAsset)
            {
                config = StoreConfiguration.Deserialize(textAsset.text);
            }

            return config;
        }
    }
}

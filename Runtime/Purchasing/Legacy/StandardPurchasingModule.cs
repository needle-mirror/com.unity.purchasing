using System;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Module for the standard stores covered by Unity;
    /// Apple App store, Google Play and more.
    /// </summary>
    [Obsolete(IAPObsoleteMessages.UpgradeToIAPV5, false)]
    public class StandardPurchasingModule
    {
        static StandardPurchasingModule instance = null;
        /// <summary>
        /// Creates an instance of StandardPurchasingModule or retrieves the existing one.
        /// </summary>
        /// <returns> The existing instance or the one just created. </returns>
        public static StandardPurchasingModule Instance()
        {
            if (instance == null)
            {
                instance = new StandardPurchasingModule();
            }
            return instance;
        }

        /// <summary>
        /// Creates an instance of StandardPurchasingModule or retrieves the existing one.
        /// For backwards compatibility only. Returns same results as calling `Instance()`.
        /// </summary>
        /// <param name="androidStore">The Android store type. This parameter is ignored and maintained only for backwards compatibility.</param>
        /// <returns> The existing instance or the one just created. </returns>
        public static StandardPurchasingModule Instance(AppStore androidStore)
        {
            return Instance();
        }

        /// <summary>
        /// The UI mode for the Fake store, if it's in use.
        /// Currently non-functional. FakeStore will use StandardUser UI Mode regardless of value.
        /// </summary>
        public FakeStoreUIMode useFakeStoreUIMode { get; set; }

        /// <summary>
        /// Whether or not to use the Fake store.
        /// Currently non-functional.
        /// </summary>
        public bool useFakeStoreAlways { get; set; }

        /// <summary>
        /// The version of com.unity.purchasing installed and the app was built using.
        /// </summary>
        public string Version => IAPVersion.Current;

        /// <summary>
        /// A property that retrieves the <c>AppStore</c> type.
        /// </summary>
        public AppStore appStore => DefaultStoreHelper.GetDefaultBuiltInAppStore();
    }
}

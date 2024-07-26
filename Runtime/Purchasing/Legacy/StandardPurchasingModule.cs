using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Module for the standard stores covered by Unity;
    /// Apple App store, Google Play and more.
    /// </summary>
    [Obsolete("Please upgrade to the new APIs available. For more info visit `Upgrading to IAP v5` in the IAP documentation. https://docs.unity3d.com/Packages/com.unity.purchasing@latest", false)]
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

        internal readonly string k_Version = "5.0.0"; // NOTE: Changed using GenerateUnifiedIAP.sh before pack step.
        /// <summary>
        /// The version of com.unity.purchasing installed and the app was built using.
        /// </summary>
        public string Version => k_Version;

        /// <summary>
        /// A property that retrieves the <c>AppStore</c> type.
        /// </summary>
        public AppStore appStore => DefaultStoreHelper.GetDefaultBuiltInAppStore();
    }
}

#nullable enable

namespace UnityEngine.Purchasing.Stores.PaymentProviderLaunchers
{
    /// <summary>
    /// Picks the platform's default <see cref="IWebViewLauncher"/>, or returns
    /// null when the platform has no built-in option (Standalone/Editor/WebGL).
    /// </summary>
    internal static class BuiltInLauncherFactory
    {
        internal static IWebViewLauncher? Resolve()
        {
#if UNITY_IOS && !UNITY_EDITOR
            return new IosInAppBrowserLauncher();
#elif UNITY_ANDROID && !UNITY_EDITOR
            return new AndroidCustomTabsLauncher();
#else
            return null;
#endif
        }
    }
}

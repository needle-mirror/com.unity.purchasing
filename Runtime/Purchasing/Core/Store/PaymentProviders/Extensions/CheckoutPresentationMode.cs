namespace UnityEngine.Purchasing
{
    /// <summary>
    /// How a payment-provider checkout URL is presented to the user.
    /// </summary>
    public enum CheckoutPresentationMode
    {
        /// <summary>
        /// Open the checkout URL in the system's external browser via
        /// <see cref="Application.OpenURL(string)"/>. The user leaves the
        /// application; order resolution resumes when focus is regained.
        /// This is the default.
        /// </summary>
        ExternalBrowser,

        /// <summary>
        /// Open the checkout URL in an in-app browser / webview using a
        /// registered <see cref="IWebViewLauncher"/> or, if none is
        /// registered, the platform's built-in launcher (iOS
        /// SFSafariViewController, Android Chrome Custom Tabs). Falls back to
        /// the external browser on platforms without a built-in launcher.
        /// </summary>
        WebView
    }
}

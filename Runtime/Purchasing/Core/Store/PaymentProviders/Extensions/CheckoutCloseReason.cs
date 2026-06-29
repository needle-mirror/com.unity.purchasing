namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Why an in-app checkout webview session ended, as reported by the
    /// <see cref="IWebViewLauncher"/>.
    /// </summary>
    public enum CheckoutCloseReason
    {
        /// <summary>The reason is not known. Order status will be polled.</summary>
        Unknown,

        /// <summary>The user explicitly dismissed the in-app browser.</summary>
        UserDismissed,

        /// <summary>
        /// A configured deep-link scheme returned the user to the app while
        /// the in-app browser was open. <see cref="CheckoutResult.FinalUrl"/>
        /// holds the deep-link URL.
        /// </summary>
        DeepLinkReturned,

        /// <summary>
        /// The launcher could not open or present the URL. The SDK falls back
        /// to <see cref="Application.OpenURL(string)"/> for this purchase.
        /// <see cref="CheckoutResult.Error"/> holds the failure message.
        /// </summary>
        LauncherFailed
    }
}

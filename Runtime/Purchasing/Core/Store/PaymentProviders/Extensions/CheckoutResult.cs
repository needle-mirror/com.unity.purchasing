#nullable enable

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Result returned by an <see cref="IWebViewLauncher"/> when the in-app
    /// checkout session ends.
    /// </summary>
    public sealed class CheckoutResult
    {
        /// <summary>Why the session ended.</summary>
        public CheckoutCloseReason CloseReason { get; }

        /// <summary>
        /// The final URL observed when the session ended. Only set when
        /// <see cref="CloseReason"/> is <see cref="CheckoutCloseReason.DeepLinkReturned"/>.
        /// </summary>
        public string? FinalUrl { get; }

        /// <summary>
        /// Failure message. Only set when <see cref="CloseReason"/> is
        /// <see cref="CheckoutCloseReason.LauncherFailed"/>.
        /// </summary>
        public string? Error { get; }

        /// <summary>Create a checkout result.</summary>
        public CheckoutResult(CheckoutCloseReason closeReason, string? finalUrl = null, string? error = null)
        {
            CloseReason = closeReason;
            FinalUrl = finalUrl;
            Error = error;
        }
    }
}

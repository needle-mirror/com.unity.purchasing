#nullable enable

using System.Threading;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Implemented by code that can present a payment-provider checkout URL
    /// inside the application (for example, in an in-app browser or a
    /// developer-supplied webview).
    /// <para>
    /// Register an instance with
    /// <see cref="IPaymentProvidersExtendedService.SetWebViewLauncher(IWebViewLauncher)"/>
    /// to override the platform's built-in launcher.
    /// </para>
    /// </summary>
    public interface IWebViewLauncher
    {
        /// <summary>
        /// Present <paramref name="url"/> to the user and complete the task
        /// when the session ends.
        /// </summary>
        /// <param name="url">The checkout URL to present.</param>
        /// <param name="ct">
        /// Cancelled by the SDK when an external signal (e.g. a deep-link
        /// return) supersedes the launcher; implementations should dismiss
        /// the in-app browser when this is signalled.
        /// </param>
        Task<CheckoutResult> LaunchAsync(string url, CancellationToken ct);
    }
}

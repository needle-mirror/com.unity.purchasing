#nullable enable

using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.Stores
{
    /// <summary>
    /// Abstraction over <see cref="DeepLinkCheckoutSignal"/> so the coordinator's
    /// deep-link race can be unit-tested with a fake signal — the concrete type
    /// hooks <c>Application.deepLinkActivated</c>, which a test can't raise.
    /// </summary>
    internal interface IDeepLinkCheckoutSignal : IDisposable
    {
        Task<string> Task { get; }
    }

    /// <summary>
    /// Listens for deep links matching a configured URL scheme while a webview
    /// checkout is open. Completes its <see cref="Task"/> with the deep-link
    /// URL the first time a matching link arrives.
    /// </summary>
    internal sealed class DeepLinkCheckoutSignal : IDeepLinkCheckoutSignal
    {
        readonly TaskCompletionSource<string> m_Tcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly string m_SchemePrefix;
        readonly Action<string> m_Handler;
        bool m_Disposed;

        DeepLinkCheckoutSignal(string scheme)
        {
            m_SchemePrefix = scheme + ":";
            m_Handler = OnDeepLink;
            Application.deepLinkActivated += m_Handler;
        }

        public Task<string> Task => m_Tcs.Task;

        internal static DeepLinkCheckoutSignal? StartIfConfigured(string? scheme)
        {
            return string.IsNullOrEmpty(scheme) ? null : new DeepLinkCheckoutSignal(scheme!);
        }

        void OnDeepLink(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            if (url.StartsWith(m_SchemePrefix, StringComparison.OrdinalIgnoreCase))
            {
                m_Tcs.TrySetResult(url);
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }
            m_Disposed = true;
            Application.deepLinkActivated -= m_Handler;
            // If nothing fired, leave the task uncompleted; it's only awaited via WhenAny.
            m_Tcs.TrySetCanceled();
        }
    }
}

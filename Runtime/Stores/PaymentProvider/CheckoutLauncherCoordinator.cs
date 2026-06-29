#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Uniject;
using UnityEngine.Purchasing.Stores.PaymentProviderLaunchers;

namespace UnityEngine.Purchasing.Stores
{
    /// <summary>
    /// Decides how a payment-provider checkout URL is presented and resolves
    /// the in-app webview / deep-link race when WebView mode is selected.
    /// One coordinator per <see cref="PaymentProviderImpl"/>.
    /// </summary>
    internal class CheckoutLauncherCoordinator
    {
        readonly IUtil m_Util;
        readonly ILogger m_Logger;
        readonly Func<IWebViewLauncher?> m_BuiltInLauncherFactory;
        readonly Func<string?, IDeepLinkCheckoutSignal?> m_DeepLinkSignalFactory;

        IWebViewLauncher? m_OverrideLauncher;
        IWebViewLauncher? m_BuiltInLauncher;
        string? m_DeepLinkScheme;
        bool m_LoggedFallback;

        internal CheckoutLauncherCoordinator(IUtil util, ILogger logger)
            : this(util, logger, BuiltInLauncherFactory.Resolve)
        {
        }

        // Test seam.
        internal CheckoutLauncherCoordinator(IUtil util, ILogger logger, Func<IWebViewLauncher?> builtInLauncherFactory,
            Func<string?, IDeepLinkCheckoutSignal?>? deepLinkSignalFactory = null)
        {
            m_Util = util;
            m_Logger = logger;
            m_BuiltInLauncherFactory = builtInLauncherFactory;
            m_DeepLinkSignalFactory = deepLinkSignalFactory ?? (scheme => DeepLinkCheckoutSignal.StartIfConfigured(scheme));
        }

        internal void SetOverrideLauncher(IWebViewLauncher? launcher)
        {
            m_OverrideLauncher = launcher;
        }

        internal void SetDeepLinkScheme(string? scheme)
        {
            m_DeepLinkScheme = string.IsNullOrEmpty(scheme) ? null : scheme;
        }

        /// <summary>
        /// Present <paramref name="url"/> using <paramref name="mode"/>. Returns when
        /// the session ends (for in-app browser mode) or immediately (for external
        /// browser mode). External browser mode always returns
        /// <see cref="CheckoutCloseReason.Unknown"/>; the caller's existing
        /// focus-driven poll handles resolution. The mode is passed per call so each
        /// launch context (payment checkout / webshop) can route independently.
        /// </summary>
        internal async Task<CheckoutResult> LaunchCheckoutAsync(string url, CheckoutPresentationMode mode)
        {
            if (mode == CheckoutPresentationMode.ExternalBrowser)
            {
                m_Util.OpenURL(url);
                return new CheckoutResult(CheckoutCloseReason.Unknown);
            }

            var launcher = ResolveWebViewLauncher();
            if (launcher == null)
            {
                if (!m_LoggedFallback)
                {
                    m_LoggedFallback = true;
                    m_Logger.LogWarning("UnityIAP",
                        $"WebView checkout requested but no IWebViewLauncher is available on platform '{Application.platform}'. Falling back to external browser.");
                }
                m_Util.OpenURL(url);
                return new CheckoutResult(CheckoutCloseReason.Unknown);
            }

            using var cts = new CancellationTokenSource();
            var deepLinkSignal = m_DeepLinkSignalFactory(m_DeepLinkScheme);

            try
            {
                Task<CheckoutResult> launcherTask;
                try
                {
                    launcherTask = launcher.LaunchAsync(url, cts.Token);
                }
                catch (Exception e)
                {
                    return FallbackOnLauncherFailure(url, e.Message);
                }

                if (deepLinkSignal == null)
                {
                    return await AwaitLauncher(launcherTask, url);
                }

                var winner = await Task.WhenAny(launcherTask, deepLinkSignal.Task);
                if (winner == deepLinkSignal.Task)
                {
                    cts.Cancel();
                    var deepLinkUrl = deepLinkSignal.Task.Result;
                    return new CheckoutResult(CheckoutCloseReason.DeepLinkReturned, finalUrl: deepLinkUrl);
                }

                return await AwaitLauncher(launcherTask, url);
            }
            finally
            {
                deepLinkSignal?.Dispose();
            }
        }

        async Task<CheckoutResult> AwaitLauncher(Task<CheckoutResult> launcherTask, string url)
        {
            try
            {
                var result = await launcherTask;
                if (result == null)
                {
                    return new CheckoutResult(CheckoutCloseReason.Unknown);
                }

                // Built-in launchers report init failures (no root view
                // controller, invalid URL, activity unavailable) by *returning*
                // a LauncherFailed result rather than throwing — so route them
                // through the same external-browser fallback as a thrown failure.
                if (result.CloseReason == CheckoutCloseReason.LauncherFailed)
                {
                    return FallbackOnLauncherFailure(url, result.Error ?? "Unknown error");
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                return new CheckoutResult(CheckoutCloseReason.Unknown);
            }
            catch (Exception e)
            {
                return FallbackOnLauncherFailure(url, e.Message);
            }
        }

        CheckoutResult FallbackOnLauncherFailure(string url, string message)
        {
            m_Logger.LogWarning("UnityIAP",
                $"IWebViewLauncher failed: {message}. Falling back to external browser.");
            m_Util.OpenURL(url);
            return new CheckoutResult(CheckoutCloseReason.LauncherFailed, error: message);
        }

        IWebViewLauncher? ResolveWebViewLauncher()
        {
            if (m_OverrideLauncher != null)
            {
                return m_OverrideLauncher;
            }

            return m_BuiltInLauncher ??= m_BuiltInLauncherFactory();
        }
    }
}

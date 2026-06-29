#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
#if UNITY_IOS && !UNITY_EDITOR
using System.Runtime.InteropServices;
using AOT;
#endif

namespace UnityEngine.Purchasing.Stores.PaymentProviderLaunchers
{
    /// <summary>
    /// Built-in <see cref="IWebViewLauncher"/> for iOS that presents the
    /// payment-provider checkout URL in an <c>SFSafariViewController</c> over
    /// Unity's root view controller.
    /// <para>
    /// The native plugin entry points are defined in
    /// <c>Plugins/UnityPurchasing/iOS/UnityPurchasingInAppBrowser.mm</c>.
    /// </para>
    /// </summary>
    internal sealed class IosInAppBrowserLauncher : IWebViewLauncher
    {
        // Reasons reported by the native callback.
        const int k_NativeReasonUserDismissed = 0;
        const int k_NativeReasonFailed = 1;

        // The coordinator serializes purchases, so at most one in-app browser
        // session is ever in flight. A single static TCS / cancellation
        // registration is sufficient.
        static TaskCompletionSource<CheckoutResult>? s_PendingCompletion;
        static CancellationTokenRegistration s_PendingCancellation;
        static readonly object s_Lock = new object();

#if UNITY_IOS && !UNITY_EDITOR
        delegate void InAppBrowserCallback(int reason);

        [DllImport("__Internal", EntryPoint = "unityPurchasing_LaunchInAppBrowser")]
        static extern void NativeLaunchInAppBrowser(string url);

        [DllImport("__Internal", EntryPoint = "unityPurchasing_DismissInAppBrowser")]
        static extern void NativeDismissInAppBrowser();

        [DllImport("__Internal", EntryPoint = "unityPurchasing_SetInAppBrowserCallback")]
        static extern void NativeSetInAppBrowserCallback(InAppBrowserCallback callback);

        // Keep the delegate alive for the lifetime of the process — the native
        // plugin holds onto the function pointer and may invoke it at any
        // time after a call to LaunchAsync.
        static readonly InAppBrowserCallback s_Callback = OnNativeCallback;
        static bool s_CallbackRegistered;

        [MonoPInvokeCallback(typeof(InAppBrowserCallback))]
        static void OnNativeCallback(int reason)
        {
            TaskCompletionSource<CheckoutResult>? tcs;
            CancellationTokenRegistration registration;
            lock (s_Lock)
            {
                tcs = s_PendingCompletion;
                registration = s_PendingCancellation;
                s_PendingCompletion = null;
                s_PendingCancellation = default;
            }

            registration.Dispose();

            if (tcs == null)
            {
                return;
            }

            switch (reason)
            {
                case k_NativeReasonUserDismissed:
                    tcs.TrySetResult(new CheckoutResult(CheckoutCloseReason.UserDismissed));
                    break;
                case k_NativeReasonFailed:
                    tcs.TrySetResult(new CheckoutResult(CheckoutCloseReason.LauncherFailed,
                        error: "Failed to present in-app browser (no root view controller available)."));
                    break;
                default:
                    tcs.TrySetResult(new CheckoutResult(CheckoutCloseReason.Unknown));
                    break;
            }
        }
#endif

        public Task<CheckoutResult> LaunchAsync(string url, CancellationToken ct)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (string.IsNullOrEmpty(url))
            {
                return Task.FromResult(new CheckoutResult(CheckoutCloseReason.LauncherFailed,
                    error: "URL is null or empty."));
            }

            var tcs = new TaskCompletionSource<CheckoutResult>(TaskCreationOptions.RunContinuationsAsynchronously);
            CancellationTokenRegistration registration = default;

            lock (s_Lock)
            {
                // Defensive: if a previous session is still tracked, complete it
                // as Unknown to avoid leaking a TCS. Should not happen because
                // the coordinator serializes purchases.
                s_PendingCompletion?.TrySetResult(new CheckoutResult(CheckoutCloseReason.Unknown));
                s_PendingCancellation.Dispose();

                s_PendingCompletion = tcs;

                if (!s_CallbackRegistered)
                {
                    NativeSetInAppBrowserCallback(s_Callback);
                    s_CallbackRegistered = true;
                }
            }

            try
            {
                NativeLaunchInAppBrowser(url);
            }
            catch (Exception e)
            {
                lock (s_Lock)
                {
                    if (ReferenceEquals(s_PendingCompletion, tcs))
                    {
                        s_PendingCompletion = null;
                        s_PendingCancellation.Dispose();
                        s_PendingCancellation = default;
                    }
                }
                return Task.FromResult(new CheckoutResult(CheckoutCloseReason.LauncherFailed,
                    error: e.Message));
            }

            // Wire the cancellation token to dismiss the native browser. The
            // native dismissal will fire OnNativeCallback, which completes the
            // TCS. As a safety net we also complete the TCS with UserDismissed
            // here so callers can resume even if the native side fails to
            // call back.
            if (ct.CanBeCanceled)
            {
                registration = ct.Register(static state =>
                {
                    var captured = (TaskCompletionSource<CheckoutResult>)state!;
                    try
                    {
                        NativeDismissInAppBrowser();
                    }
                    catch
                    {
                        // ignore
                    }

                    captured.TrySetResult(new CheckoutResult(CheckoutCloseReason.UserDismissed));

                    lock (s_Lock)
                    {
                        if (ReferenceEquals(s_PendingCompletion, captured))
                        {
                            s_PendingCompletion = null;
                            s_PendingCancellation.Dispose();
                            s_PendingCancellation = default;
                        }
                    }
                }, tcs);

                lock (s_Lock)
                {
                    if (ReferenceEquals(s_PendingCompletion, tcs))
                    {
                        s_PendingCancellation = registration;
                    }
                    else
                    {
                        // The TCS was already completed; dispose the registration we just made.
                        registration.Dispose();
                    }
                }
            }

            return tcs.Task;
#else
            return Task.FromResult(new CheckoutResult(CheckoutCloseReason.LauncherFailed,
                error: "iOS in-app browser is only available on iOS devices"));
#endif
        }
    }
}

#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.Stores.PaymentProviderLaunchers
{
    /// <summary>
    /// Built-in Android <see cref="IWebViewLauncher"/> that opens the checkout
    /// URL in a Chrome Custom Tab without requiring the
    /// <c>androidx.browser</c> AAR.
    /// <para>
    /// The launcher constructs an <c>ACTION_VIEW</c> intent with the standard
    /// Chrome Custom Tabs <c>extra.SESSION</c> extra (a null binder) so that
    /// Custom Tabs–aware browsers honor it. If no Custom Tabs–aware browser is
    /// installed, the intent resolves to whatever default browser handles
    /// <c>ACTION_VIEW</c>, which is the natural fallback.
    /// </para>
    /// <para>
    /// Because we don't open a Custom Tabs <c>CustomTabsSession</c>, no native
    /// close callback is available. The launcher instead listens for
    /// <see cref="Application.focusChanged"/>: the first focus-loss event is
    /// the launch itself; the next focus-regain is treated as the user
    /// returning to the app (browser closed) and the task completes with
    /// <see cref="CheckoutCloseReason.UserDismissed"/>.
    /// </para>
    /// </summary>
    internal sealed class AndroidCustomTabsLauncher : IWebViewLauncher
    {
        public Task<CheckoutResult> LaunchAsync(string url, CancellationToken ct)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var tcs = new TaskCompletionSource<CheckoutResult>(TaskCreationOptions.RunContinuationsAsynchronously);

            AndroidJavaObject? activity;
            try
            {
                activity = UnityActivity.GetCurrentActivity();
            }
            catch (Exception e)
            {
                tcs.TrySetResult(new CheckoutResult(
                    CheckoutCloseReason.LauncherFailed,
                    error: $"Failed to obtain Android current activity: {e.Message}"));
                return tcs.Task;
            }

            if (activity == null)
            {
                tcs.TrySetResult(new CheckoutResult(
                    CheckoutCloseReason.LauncherFailed,
                    error: "Android current activity is null."));
                return tcs.Task;
            }

            try
            {
                StartCustomTabsIntent(activity, url);
            }
            catch (Exception e)
            {
                tcs.TrySetResult(new CheckoutResult(
                    CheckoutCloseReason.LauncherFailed,
                    error: e.Message));
                return tcs.Task;
            }
            finally
            {
                // Release the JNI local reference; the focus-driven close
                // detection below does not need the activity.
                activity.Dispose();
            }

            // Subscribe to focus changes to detect close. Skip the first event
            // (focus-loss caused by launching the Custom Tab itself).
            var sawFirstEvent = false;
            CancellationTokenRegistration ctr = default;

            void Complete(CheckoutResult result)
            {
                Application.focusChanged -= OnFocusChanged;
                tcs.TrySetResult(result);
            }

            void OnFocusChanged(bool hasFocus)
            {
                if (!sawFirstEvent)
                {
                    sawFirstEvent = true;
                    return;
                }

                if (hasFocus)
                {
                    Complete(new CheckoutResult(CheckoutCloseReason.UserDismissed));
                }
            }

            Application.focusChanged += OnFocusChanged;

            if (ct.CanBeCanceled)
            {
                ctr = ct.Register(() =>
                {
                    Complete(new CheckoutResult(CheckoutCloseReason.UserDismissed));
                });

                // Dispose the registration once the session completes rather than
                // inside Complete: if the token is already cancelled at Register
                // time the callback runs synchronously before `ctr` is assigned,
                // so disposing there would be a no-op and leak the registration.
                tcs.Task.ContinueWith(_ => ctr.Dispose(), TaskContinuationOptions.ExecuteSynchronously);
            }

            return tcs.Task;
#else
            return Task.FromResult(new CheckoutResult(
                CheckoutCloseReason.LauncherFailed,
                error: "Android Custom Tabs launcher is only available on Android devices"));
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        const string k_ActionView = "android.intent.action.VIEW";
        const string k_CustomTabsSessionExtra = "android.support.customtabs.extra.SESSION";
        const int k_FlagActivityNewTask = 0x10000000;

        static void StartCustomTabsIntent(AndroidJavaObject activity, string url)
        {
            using var uriClass = new AndroidJavaClass("android.net.Uri");
            using var uri = uriClass.CallStatic<AndroidJavaObject>("parse", url);

            using var intent = new AndroidJavaObject("android.content.Intent", k_ActionView, uri);

            // Build a Bundle with a null binder under the Custom Tabs session
            // key. This is the marker Chrome (and other Custom Tabs–aware
            // browsers) look for to open the URL as a Custom Tab.
            using var bundle = new AndroidJavaObject("android.os.Bundle");
            bundle.Call("putBinder", k_CustomTabsSessionExtra, (AndroidJavaObject?)null);

            // Both putExtras(Bundle) and addFlags(int) return Intent; we don't
            // need the return value, so call them non-generically and dispose
            // any returned object via the using-friendly overload.
            using (intent.Call<AndroidJavaObject>("putExtras", bundle)) { }
            using (intent.Call<AndroidJavaObject>("addFlags", k_FlagActivityNewTask)) { }

            activity.Call("startActivity", intent);
        }
#endif
    }
}

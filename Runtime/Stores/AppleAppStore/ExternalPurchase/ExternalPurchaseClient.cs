#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using AOT;
using UnityEngine.Purchasing.MiniJSON;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Standalone client for Apple's ExternalPurchaseCustomLink API (EU compliance - iOS 18.1+ / macOS 15.1+ / visionOS 2.1+).
    /// Use <c>new ExternalPurchaseClient()</c> to create an instance.
    /// See <a href="https://developer.apple.com/support/communication-and-promotion-of-offers-on-the-app-store-in-the-eu/">Apple's EU External Purchase guide</a>.
    /// </summary>
    public class ExternalPurchaseClient
    {
        // Static callback fields — one set per operation
        static Action<bool>? s_CheckEligibilitySuccess;
        static Action<string>? s_CheckEligibilityError;
        static Action<string?, string>? s_FetchTokenSuccess;
        static Action<string>? s_FetchTokenError;
        static Action? s_ShowNoticeSuccess;
        static Action<string>? s_ShowNoticeError;
        static Action<string>? s_FetchStorefrontSuccess;
        static Action<string>? s_FetchStorefrontError;

        static INativeAppleStore? s_NativeStore;

        static INativeAppleStore? NativeStore
        {
            get
            {
                if (s_NativeStore == null)
                {
                    var nativeStoreProvider = new NativeStoreProvider();
                    s_NativeStore = nativeStoreProvider.GetStorekit();
                }
                return s_NativeStore;
            }
        }

#if UNITY_IOS && !UNITY_EDITOR
        static string ConvertPtrToString(IntPtr ptr)
        {
            var str = "";
            if (ptr != IntPtr.Zero)
            {
                str = Marshal.PtrToStringAuto(ptr) ?? string.Empty;
                NativeStore?.DeallocateMemory(ptr);
            }
            return str;
        }

        [MonoPInvokeCallback(typeof(ExternalPurchaseCallback))]
        static void NativeCallback(IntPtr subjectPtr, IntPtr payloadPtr)
        {
            var subject = ConvertPtrToString(subjectPtr);
            var payload = ConvertPtrToString(payloadPtr);

            switch (subject)
            {
                case "OnCheckExternalPurchaseEligibilitySucceeded":
                    OnCheckEligibilitySucceeded(payload);
                    break;
                case "OnCheckExternalPurchaseEligibilityFailed":
                    s_CheckEligibilityError?.Invoke(payload);
                    break;
                case "OnFetchExternalPurchaseTokenSucceeded":
                    OnFetchTokenSucceeded(payload);
                    break;
                case "OnFetchExternalPurchaseTokenFailed":
                    s_FetchTokenError?.Invoke(payload);
                    break;
                case "OnShowExternalPurchaseNoticeSucceeded":
                    s_ShowNoticeSuccess?.Invoke();
                    break;
                case "OnShowExternalPurchaseNoticeFailed":
                    s_ShowNoticeError?.Invoke(payload);
                    break;
                case "OnFetchStorefrontSucceeded":
                    OnFetchStorefrontSucceeded(payload);
                    break;
                case "OnFetchStorefrontFailed":
                    s_FetchStorefrontError?.Invoke(payload);
                    break;
            }
        }

        static void OnCheckEligibilitySucceeded(string json)
        {
            var data = Json.Deserialize(json) as Dictionary<string, object>;
            var isEligible = false;
            if (data != null && data.TryGetValue("isEligible", out var value) && value is bool b)
            {
                isEligible = b;
            }
            s_CheckEligibilitySuccess?.Invoke(isEligible);
        }

        static void OnFetchTokenSucceeded(string json)
        {
            var data = Json.Deserialize(json) as Dictionary<string, object>;
            string? token = null;
            var tokenType = "";
            if (data != null)
            {
                if (data.TryGetValue("token", out var t))
                {
                    token = t as string;
                }
                if (data.TryGetValue("tokenType", out var tt))
                {
                    tokenType = tt as string ?? "";
                }
            }
            s_FetchTokenSuccess?.Invoke(token, tokenType);
        }

        static void OnFetchStorefrontSucceeded(string json)
        {
            var data = Json.Deserialize(json) as Dictionary<string, object>;
            var countryCode = "";
            if (data != null && data.TryGetValue("countryCode", out var value))
            {
                countryCode = value as string ?? "";
            }
            s_FetchStorefrontSuccess?.Invoke(countryCode);
        }
#endif

        /// <summary>
        /// Creates a new standalone ExternalPurchaseClient.
        /// No IAP service initialization required.
        /// </summary>
        public ExternalPurchaseClient()
        {
        }

        /// <summary>
        /// Check if external purchase is available for this user/region.
        /// Only available with StoreKit 2 on iOS 18.1+ / macOS 15.1+ / visionOS 2.1+.
        /// Returns true for EU App Store users, false otherwise.
        /// </summary>
        /// <param name="successCallback">Called with true if eligible, false otherwise.</param>
        /// <param name="errorCallback">Called with an error message if the operation fails (e.g., unsupported OS).</param>
        ///
        /// Made private, users are expected to use CheckEligibilityAsync instead which returns a Task.
        private void CheckEligibility(Action<bool> successCallback, Action<string> errorCallback)
        {
#if UNITY_IOS && !UNITY_EDITOR
            s_CheckEligibilitySuccess = successCallback;
            s_CheckEligibilityError = errorCallback;
            NativeStore?.ExternalPurchaseCheckEligibility(NativeCallback);
#else
            errorCallback?.Invoke("ExternalPurchaseClient is only supported on iOS devices");
#endif
        }

        /// <summary>
        /// Fetch an external purchase token to associate with the customer account.
        /// Send this token to your backend when constructing the purchase URL.
        /// Only available with StoreKit 2 on iOS 18.1+ / macOS 15.1+ / visionOS 2.1+.
        /// </summary>
        /// <param name="tokenType">The type of token to fetch.</param>
        /// <param name="successCallback">Called with the token and token type when successful.</param>
        /// <param name="errorCallback">Called with an error message if the operation fails.</param>
        ///
        /// Made private, users are expected to use FetchTokenAsync instead which returns a Task.
        private void FetchToken(ExternalPurchaseTokenType tokenType, Action<string?, string> successCallback, Action<string> errorCallback)
        {
#if UNITY_IOS && !UNITY_EDITOR
            s_FetchTokenSuccess = successCallback;
            s_FetchTokenError = errorCallback;
            NativeStore?.ExternalPurchaseFetchToken(ConvertTokenType(tokenType), NativeCallback);
#else
            errorCallback?.Invoke("ExternalPurchaseClient is only supported on iOS devices");
#endif
        }

        /// <summary>
        /// Show Apple's required notice before linking to an external purchase.
        /// Must be called after deliberate user interaction (e.g., button tap).
        /// After success, open your purchase URL using Application.OpenURL().
        /// Only available with StoreKit 2 on iOS 18.1+ / macOS 15.1+ / visionOS 2.1+.
        /// </summary>
        /// <param name="noticeType">Determines how the external purchase link will be opened.</param>
        /// <param name="successCallback">Called when the notice is shown successfully.</param>
        /// <param name="errorCallback">Called with an error message if the operation fails.</param>
        ///
        /// Made private, users are expected to use ShowNoticeAsync instead which returns a Task.
        private void ShowNotice(ExternalPurchaseNoticeType noticeType, Action successCallback, Action<string> errorCallback)
        {
#if UNITY_IOS && !UNITY_EDITOR
            s_ShowNoticeSuccess = successCallback;
            s_ShowNoticeError = errorCallback;
            NativeStore?.ExternalPurchaseShowNotice(ConvertNoticeType(noticeType), NativeCallback);
#else
            errorCallback?.Invoke("ExternalPurchaseClient is only supported on iOS devices");
#endif
        }

        /// <summary>
        /// Async version of <see cref="CheckEligibility"/>.
        /// Returns true if the user is eligible for external purchases (EU region), false otherwise.
        /// </summary>
        public Task<bool> CheckEligibilityAsync()
        {
            var tcs = new TaskCompletionSource<bool>();
            CheckEligibility(
                isEligible => tcs.TrySetResult(isEligible),
                error => tcs.TrySetException(new Exception(error))
            );
            return tcs.Task;
        }

        /// <summary>
        /// Async version of <see cref="FetchToken"/>.
        /// Returns a tuple of (token, tokenType) on success.
        /// </summary>
        public Task<(string? token, string tokenType)> FetchTokenAsync(ExternalPurchaseTokenType tokenType)
        {
            var tcs = new TaskCompletionSource<(string?, string)>();
            FetchToken(tokenType,
                (token, type) => tcs.TrySetResult((token, type)),
                error => tcs.TrySetException(new Exception(error))
            );
            return tcs.Task;
        }

        /// <summary>
        /// Async version of <see cref="ShowNotice"/>.
        /// Completes when the user accepts the notice.
        /// </summary>
        public Task ShowNoticeAsync(ExternalPurchaseNoticeType noticeType)
        {
            var tcs = new TaskCompletionSource<bool>();
            ShowNotice(noticeType,
                () => tcs.TrySetResult(true),
                error => tcs.TrySetException(new Exception(error))
            );
            return tcs.Task;
        }

        /// <summary>
        /// Fetch the current App Store storefront country code.
        /// Used for region-based token type selection (EU vs Japan).
        /// </summary>
        /// <param name="successCallback">Called with the 3-letter ISO country code (e.g. "JPN", "DEU").</param>
        /// <param name="errorCallback">Called with an error message if the operation fails.</param>
        ///
        /// Made private, users are expected to use FetchStorefrontAsync instead which returns a Task.
        private void FetchStorefront(Action<string> successCallback, Action<string> errorCallback)
        {
#if UNITY_IOS && !UNITY_EDITOR
            s_FetchStorefrontSuccess = successCallback;
            s_FetchStorefrontError = errorCallback;
            if (NativeStore != null)
            {
                NativeStore.ExternalPurchaseFetchStorefront(NativeCallback);
            }
            else
            {
                errorCallback?.Invoke("Native store is not available");
            }
#else
            errorCallback?.Invoke("ExternalPurchaseClient is only supported on iOS devices");
#endif
        }

        /// <summary>
        /// Async version of <see cref="FetchStorefront"/>.
        /// Returns the 3-letter ISO country code (e.g. "JPN", "DEU").
        /// </summary>
        public Task<string> FetchStorefrontAsync()
        {
            var tcs = new TaskCompletionSource<string>();
            FetchStorefront(
                countryCode => tcs.TrySetResult(countryCode),
                error => tcs.TrySetException(new Exception(error))
            );
            return tcs.Task;
        }

        static string ConvertTokenType(ExternalPurchaseTokenType tokenType) => tokenType switch
        {
            ExternalPurchaseTokenType.Acquisition => "ACQUISITION",
            ExternalPurchaseTokenType.Services => "SERVICES",
            ExternalPurchaseTokenType.LinkOut => "LINK_OUT",
            _ => throw new ArgumentOutOfRangeException(nameof(tokenType), tokenType, null)
        };

        static string ConvertNoticeType(ExternalPurchaseNoticeType noticeType) => noticeType switch
        {
            ExternalPurchaseNoticeType.Browser => "BROWSER",
            ExternalPurchaseNoticeType.WithinApp => "WITHINAPP",
            _ => throw new ArgumentOutOfRangeException(nameof(noticeType), noticeType, null)
        };
    }
}

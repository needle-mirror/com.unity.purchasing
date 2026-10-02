#nullable enable
using System.Threading.Tasks;
using UnityEngine.Scripting;
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using AOT;
#endif

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Implemented by Firebase clients that can report whether the default app
    /// is configured.
    /// <see cref="Stores.PlayerData"/> caches ids only once the app is configured.
    /// </summary>
    internal interface IFirebaseDefaultAppState
    {
        bool IsDefaultAppConfigured { get; }
    }

    /// <summary>
    /// Apple counterpart of the Android <see cref="FirebaseAnalyticsClient"/>.
    /// The native side (<c>Plugins/UnityPurchasing/iOS/UnityFirebaseAnalytics.m</c>)
    /// uses ObjC runtime reflection, so Firebase is optional: every field resolves
    /// to null when Firebase is not shipped in the build or is too old for the API.
    /// Until Firebase is configured, the session id is also null and the app id
    /// comes from the bundled plist.
    /// </summary>
    [Preserve]
    internal class AppleFirebaseAnalyticsClient : IFirebaseAnalyticsClient, IFirebaseDefaultAppState
    {
#if (UNITY_IOS || UNITY_TVOS) && !UNITY_EDITOR
        delegate void FirebaseSessionIdCallback(string? sessionId, [MarshalAs(UnmanagedType.I1)] bool timedOut);

        [DllImport("__Internal", EntryPoint = "unityPurchasingFirebaseAppInstanceId")]
        static extern string? NativeAppInstanceId();

        [DllImport("__Internal", EntryPoint = "unityPurchasingFirebaseAppId")]
        static extern string? NativeAppId();

        [DllImport("__Internal", EntryPoint = "unityPurchasingFirebaseBundledAppId")]
        static extern string? NativeBundledAppId();

        [DllImport("__Internal", EntryPoint = "unityPurchasingFirebaseIsDefaultAppConfigured")]
        [return: MarshalAs(UnmanagedType.I1)]
        static extern bool NativeIsDefaultAppConfigured();

        [DllImport("__Internal", EntryPoint = "unityPurchasingFirebaseSessionId")]
        static extern void NativeFetchSessionId(FirebaseSessionIdCallback callback);

        // The pending session id request, shared by concurrent callers until the
        // native callback fires.
        static TaskCompletionSource<(string? sessionId, bool timedOut)>? s_PendingSessionId;
        static readonly object s_Lock = new object();

        // Keep the delegate alive — the native completion may fire after this
        // call returns, on any thread.
        static readonly FirebaseSessionIdCallback s_SessionIdCallback = OnSessionId;

        [MonoPInvokeCallback(typeof(FirebaseSessionIdCallback))]
        static void OnSessionId(string? sessionId, bool timedOut)
        {
            TaskCompletionSource<(string? sessionId, bool timedOut)>? tcs;
            lock (s_Lock)
            {
                tcs = s_PendingSessionId;
                s_PendingSessionId = null;
            }
            tcs?.TrySetResult((sessionId, timedOut));
        }

        public bool IsDefaultAppConfigured
        {
            get
            {
                try
                {
                    return NativeIsDefaultAppConfigured();
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        // Every fetch degrades to null rather than throwing: these ids are optional,
        // and an exception here would fail identity creation and with it Payment
        // Provider order creation.
        public Task<string?> FetchAppInstanceIdAsync()
        {
            try
            {
                return Task.FromResult(NativeAppInstanceId());
            }
            catch (Exception)
            {
                return Task.FromResult<string?>(null);
            }
        }

        public Task<string?> FetchAppIdAsync()
        {
            try
            {
                // The configured app's id, or before configure the bundled plist's.
                return Task.FromResult(NativeAppId() ?? NativeBundledAppId());
            }
            catch (Exception)
            {
                return Task.FromResult<string?>(null);
            }
        }

        public Task<(string? sessionId, bool timedOut)> FetchSessionIdAsync()
        {
            TaskCompletionSource<(string? sessionId, bool timedOut)> tcs;
            lock (s_Lock)
            {
                if (s_PendingSessionId != null)
                {
                    return s_PendingSessionId.Task;
                }
                tcs = s_PendingSessionId = new TaskCompletionSource<(string? sessionId, bool timedOut)>();
            }
            try
            {
                NativeFetchSessionId(s_SessionIdCallback);
            }
            catch (Exception)
            {
                // Release the pending request, or every later caller would be
                // handed this one and wait on a callback that will never come.
                lock (s_Lock)
                {
                    if (s_PendingSessionId == tcs)
                    {
                        s_PendingSessionId = null;
                    }
                }
                tcs.TrySetResult((null, false));
            }
            return tcs.Task;
        }
#else
        // Editor, macOS, visionOS and non-Apple players: Firebase identifiers are not collected.
        // macOS would need the prebuilt Externals/osx bundle rebuilt. Google Analytics for Firebase
        // does not officially support visionOS.
        public bool IsDefaultAppConfigured => false;
        public Task<string?> FetchAppInstanceIdAsync() => Task.FromResult<string?>(null);
        public Task<string?> FetchAppIdAsync() => Task.FromResult<string?>(null);
        public Task<(string? sessionId, bool timedOut)> FetchSessionIdAsync() =>
            Task.FromResult<(string? sessionId, bool timedOut)>((null, false));
#endif
    }
}

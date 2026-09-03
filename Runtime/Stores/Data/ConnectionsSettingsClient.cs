#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;
using UnityEngine.Purchasing.Registration;

namespace UnityEngine.Purchasing.Stores
{
    internal class ConnectionsSettingsClient
    {
        const string k_StagingEnvironment = "staging";
        const string k_Path = "/v1/get-connections-settings";

        // Attribute keys, aligned with the Insights event schema.
        internal const string GaidKey = "gaid";
        internal const string IdfaKey = "idfa";
        internal const string AppInstanceIdKey = "app_instance_id";
        internal const string FirebaseSessionIdKey = "firebase_session_id";
        internal const string FirebaseAppIdKey = "firebase_app_id";

        static readonly HashSet<string> k_Empty = new HashSet<string>();

        // Session-global cache: the settings are per-app-session, and several
        // PlayerData instances (session events + one per store) each hold
        // their own client, so the cache is shared across all of them.
        static HashSet<string>? s_Cached;
        static bool s_FetchInProgress;

        readonly ICoreRegistryHelper m_CoreRegistry;

        internal ConnectionsSettingsClient(ICoreRegistryHelper coreRegistry)
        {
            m_CoreRegistry = coreRegistry;
        }

        // Non-blocking: returns the fetched settings when available, an empty
        // set (collect nothing — fail-closed) while they are not. A miss kicks
        // off a background fetch so a later call can succeed; callers never
        // wait on the network.
        public HashSet<string> CachedEnabledAttributes
        {
            get
            {
                if (s_Cached != null)
                {
                    return s_Cached;
                }
                EnsureFetched();
                return s_Cached ?? k_Empty;
            }
        }

        // Starts a background fetch unless one already succeeded or is in
        // flight. Fire-and-forget: called at IAP core init to warm the cache
        // before the first purchase, and again on each cache miss (failed
        // fetches retry on the next call).
        public void EnsureFetched()
        {
            if (s_Cached == null && !s_FetchInProgress)
            {
                _ = FetchAndCacheAsync();
            }
        }

        async Task FetchAndCacheAsync()
        {
            s_FetchInProgress = true;
            try
            {
                var result = await FetchEnabledAttributesAsync();
                if (result != null)
                {
                    s_Cached = result;
                }
            }
            finally
            {
                s_FetchInProgress = false;
            }
        }

        internal static void ResetForTests()
        {
            s_Cached = null;
            s_FetchInProgress = false;
        }

        // Overridable for tests; returns null on any failure (never throws).
        protected virtual async Task<HashSet<string>?> FetchEnabledAttributesAsync()
        {
            try
            {
                var projectId = m_CoreRegistry.CloudProjectId;
                var installationId = m_CoreRegistry.InstallationId;
                if (string.IsNullOrEmpty(projectId) || string.IsNullOrEmpty(installationId))
                {
                    return null;
                }

                var body = MiniJson.JsonEncode(new Dictionary<string, object>
                {
                    ["projectId"] = projectId!,
                    ["installationId"] = installationId!,
                    ["platform"] = Application.platform.ToString(),
                    ["appVersion"] = Application.version,
                    ["editorVersion"] = Application.unityVersion,
                    ["sdkVersion"] = IAPVersion.Current
                });

                var response = await PostAsync(ResolveHost() + k_Path, body);
                return response == null ? null : ParseEnabledAttributes(response);
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogIAPVerbose($"get-connections-settings fetch failed: {e.Message}");
                return null;
            }
        }

        internal static HashSet<string>? ParseEnabledAttributes(string json)
        {
            if (!(MiniJson.JsonDecode(json) is Dictionary<string, object> root))
            {
                return null;
            }

            var enabled = new HashSet<string>();
            if (root.TryGetValue("attributeOverride", out var overrides)
                && overrides is Dictionary<string, object> map)
            {
                foreach (var entry in map)
                {
                    if (entry.Value is bool collect && collect)
                    {
                        enabled.Add(entry.Key);
                    }
                }
            }
            return enabled;
        }

        static Task<string?> PostAsync(string url, string body)
        {
            var tcs = new TaskCompletionSource<string?>();
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(body)),
                downloadHandler = new DownloadHandlerBuffer(),
                timeout = 10
            };
            request.SetRequestHeader("Content-Type", "application/json");

            var op = request.SendWebRequest();
            op.completed += _ =>
            {
                tcs.TrySetResult(request.result == UnityWebRequest.Result.Success
                    ? request.downloadHandler.text
                    : null);
                request.Dispose();
            };
            return tcs.Task;
        }

        // Null cloud environment (core services not initialized, or key
        // unset) defaults to production.
        internal string ResolveHost()
        {
            return m_CoreRegistry.CloudEnvironment == k_StagingEnvironment
                ? "https://insights-staging.unity3d.com"
                : "https://insights.unity3d.com";
        }
    }
}

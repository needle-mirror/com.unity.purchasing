namespace UnityEditor.Purchasing
{
    class GoogleConfigurationData
    {
        const string k_TrackingStateSessionKey = "com.unity.purchasing.GooglePlayRevenueTrackingKeyState";
        const string k_CachedProjectIdSessionKey = "com.unity.purchasing.GooglePlayKeyCachedProjectId";

        // What the obfuscator field holds, which the user can edit.
        internal string googlePlayKey;
        // The key last fetched from the cloud project, restored into the field whenever the page is reopened.
        internal string cloudGooglePlayKey;
        internal bool googlePlayTangleFileCreated;

        // Survives domain reloads so the Android build check doesn't refetch on every script compile.
        internal GooglePlayRevenueTrackingKeyState revenueTrackingState
        {
            get => (GooglePlayRevenueTrackingKeyState)SessionState.GetInt(k_TrackingStateSessionKey, (int)GooglePlayRevenueTrackingKeyState.Unknown);
            set => SessionState.SetInt(k_TrackingStateSessionKey, (int)value);
        }

        // The cache belongs to one cloud project, so relinking to another must not reuse its key or state.
        internal void ResetIfCloudProjectChanged()
        {
            var projectId = CloudProjectSettings.projectId ?? string.Empty;
            var cachedProjectId = SessionState.GetString(k_CachedProjectIdSessionKey, null);
            if (cachedProjectId == projectId)
            {
                return;
            }

            SessionState.SetString(k_CachedProjectIdSessionKey, projectId);
            if (cachedProjectId == null)
            {
                return;
            }

            // A key the user typed themselves is kept; only the previous project's cloud key goes.
            if (googlePlayKey == cloudGooglePlayKey)
            {
                googlePlayKey = null;
            }

            cloudGooglePlayKey = null;
            revenueTrackingState = GooglePlayRevenueTrackingKeyState.Unknown;
        }
    }
}

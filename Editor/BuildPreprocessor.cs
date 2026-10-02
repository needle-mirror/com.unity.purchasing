using System;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
#if UNITY_6000_3_OR_NEWER
    class BuildPreprocessor : IPreprocessBuildWithContext, IActiveBuildTargetChanged
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildCallbackContext context)
        {
            // Store stripping changes plugin import settings, which only matter to a player.
            if (!context.IsPlayerBuild)
            {
                return;
            }

            OnPreprocessPlayerBuild(context.Report.summary.platform);
        }
#else
    class BuildPreprocessor : IPreprocessBuildWithReport, IActiveBuildTargetChanged
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            OnPreprocessPlayerBuild(report.summary.platform);
        }
#endif

        const string k_IgnoreInstructions = "To hide this warning, check \"Ignore Google license key warning in the editor logs\" " +
            "in Project Settings > Services > In-App Purchasing > Google Play.";

        static void OnPreprocessPlayerBuild(BuildTarget target)
        {
            switch (target)
            {
                case BuildTarget.Android:
                    UnityPurchasingEditor.ConfigureAndroidStoresForBuild();
                    WarnIfGoogleLicenseKeyIsMissing();
                    break;
            }
        }

        public void OnActiveBuildTargetChanged(BuildTarget previousTarget, BuildTarget newTarget)
        {
            if (newTarget == BuildTarget.Android)
            {
                FetchGoogleLicenseKeyState();
            }
        }

        [InitializeOnLoadMethod]
        static void FetchGoogleLicenseKeyStateOnLoad()
        {
            // The state is kept in SessionState, so this fetches once per editor session rather than on every domain reload.
            if (EditorUserBuildSettings.activeBuildTarget == BuildTarget.Android &&
                GoogleConfigService.Instance().GoogleConfigData.revenueTrackingState == GooglePlayRevenueTrackingKeyState.Unknown)
            {
                FetchGoogleLicenseKeyState();
            }
        }

        static void FetchGoogleLicenseKeyState()
        {
            if (Application.isBatchMode || !CloudProjectSettings.projectBound)
            {
                return;
            }

            var data = GoogleConfigService.Instance().GoogleConfigData;
            new GoogleConfigurationWebRequests((key, state) =>
            {
                data.revenueTrackingState = state;
                if (!string.IsNullOrEmpty(key))
                {
                    data.cloudGooglePlayKey = key;
                }
            }).RequestRetrieveKeyOperation();
        }

        static void WarnIfGoogleLicenseKeyIsMissing()
        {
            var state = GoogleConfigService.Instance().GoogleConfigData.revenueTrackingState;

            // A build can't wait for the fetch, so it warns from the cached state and refreshes it for the next build.
            // Verified is final (the dashboard can't clear a key), so only an unconfirmed state is fetched again:
            // NoKey, a failed fetch, or Unknown after a relink.
            if (state != GooglePlayRevenueTrackingKeyState.Verified)
            {
                FetchGoogleLicenseKeyState();
            }

            var warning = GetGoogleLicenseKeyWarning(IapEditorProjectSettings.IgnoreGoogleLicenseKeyWarning,
                CloudProjectSettings.projectBound, state, TryBuildCloudProjectSettingsUri());
            if (warning != null)
            {
                Debug.unityLogger.LogIAPWarning(warning);
            }
        }

        // An unconfirmed state (not fetched yet, offline, not signed in) stays silent, so CI builds without
        // credentials don't warn about something nobody on that machine can act on.
        internal static string GetGoogleLicenseKeyWarning(bool ignored, bool projectLinked, GooglePlayRevenueTrackingKeyState state, string cloudProjectSettingsUri)
        {
            if (ignored)
            {
                return null;
            }

            if (!projectLinked)
            {
                return "This project is not linked to a Unity Cloud project, so it has no Google Play license key. " +
                    "Link it in Project Settings > Services > In-App Purchasing, then enter your Google Play license key " +
                    "in your cloud project settings so Google Play transactions can be verified. " + k_IgnoreInstructions;
            }

            if (state == GooglePlayRevenueTrackingKeyState.NoKey)
            {
                // The Console opens absolute URLs in <a href> tags when clicked.
                var link = cloudProjectSettingsUri == null ? "" : $" (<a href=\"{cloudProjectSettingsUri}\">{cloudProjectSettingsUri}</a>)";
                return "No Google Play license key is set for this project, so Google Play transactions can't be verified. " +
                    $"Enter it in your cloud project settings{link} " +
                    "or with \"Push License Key to Cloud\" in Project Settings > Services > In-App Purchasing > Google Play. " +
                    k_IgnoreInstructions;
            }

            return null;
        }

        // A warning must never be what fails a build, and the organization may not be resolved yet.
        static string TryBuildCloudProjectSettingsUri()
        {
            try
            {
                return GooglePlayConfigurationSettingsBlock.BuildCloudProjectSettingsUri();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

using System;
using Unity.Services.Core.Editor.OrganizationHandler;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// Unity IAP Client-Side Receipt Validation obfuscation window.
    /// </summary>
    /// <remarks>
    /// Collects certificate details for supported stores.
    /// Generates .cs file in Assets/, used by Unity IAP Receipt Validation.
    /// </remarks>
    internal class ObfuscatorWindow : RichEditorWindow
    {
        // Localize me
        private const string kLabelTitle = "Receipt Validation Obfuscator";

        private const string kLabelGenerateGoogle = "Obfuscate Google Play License Key";

        private const string kLabelGoogleKey = "Google Play Public License Key";
        private const string kPublicKeyPlaceholder = "--Paste Public Key Here--";
        private const string kPublicKeyLoading = "--Loading Public Key from Settings--";

        private const string kLabelGoogleInstructions =
            "Follow these four steps to set up receipt validation for Google Play.";

        private const string kLabelGooglePlayDeveloperConsoleInstructions =
            "1. Get your license key from the Google Play Developer Console:";

        private const string kLabelGooglePlayDeveloperConsoleLink = "\tOpen Google Play Developer Console";
        private const string kGooglePlayDevConsoleURL = "https://play.google.com/apps/publish/";

        private const string kLabelGooglePlayDeveloperConsoleSteps =
            "\ta. Select your app from the list\n" +
            "\tb. Go to \"Monetization setup\" under \"Monetize\"\n" +
            "\tc. Copy the key from the \"Licensing\" section";

        private const string kLabelGooglePasteKeyInstructions = "2. Paste the key here:";

        private const string kObfuscateKeyInstructions =
            "3. Obfuscate the key. (Creates Tangle classes in your project.)";

        private const string kDashboardInstructions =
            "4. To ensure correct revenue data, enter your key in the Analytics dashboard.";

        private const string kLabelDashboardLink = "\tOpen Analytics Dashboard";

        private GUIStyle m_ErrorStyle;
        private string m_GoogleError;
        private string m_AppleError;

        /// <summary>
        /// The current Google Play Public Key, in string
        /// </summary>
        string m_GooglePlayPublicKey = kPublicKeyLoading;

#if !ENABLE_EDITOR_GAME_SERVICES
        [MenuItem(IapMenuConsts.MenuItemRoot + "/Receipt Validation Obfuscator...", false, 200)]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            var window = (ObfuscatorWindow)GetWindow(typeof(ObfuscatorWindow));
            window.titleContent.text = kLabelTitle;
            window.minSize = new Vector2(340, 180);
            window.Show();

            GenericEditorMenuItemClickEventSenderHelpers.SendIapMenuOpenObfuscatorEvent();
            GameServicesEventSenderHelpers.SendTopMenuReceiptValidationObfuscatorEvent();
        }
#endif

        internal SettingsWebRequests SettingsWebRequest;

        void OnGetIAPSettings(IapSettings settings)
        {
            m_GooglePlayPublicKey = settings.google?.publicKey ?? kPublicKeyPlaceholder;
        }

        void OnGUI()
        {
            if (m_ErrorStyle == null)
            {
                m_ErrorStyle = new GUIStyle();
                m_ErrorStyle.normal.textColor = Color.red;
            }

            // Apple error message, if any
            if (!string.IsNullOrEmpty(m_AppleError))
            {
                GUILayout.Label(m_AppleError, m_ErrorStyle);
            }

            // Google Play
            GUILayout.Label(kLabelGoogleKey, EditorStyles.boldLabel);
            GUILayout.Label(kLabelGoogleInstructions);
            GUILayout.Space(5);

            GUILayout.Label(kLabelGooglePlayDeveloperConsoleInstructions);
            GUILink(kLabelGooglePlayDeveloperConsoleLink, kGooglePlayDevConsoleURL);

            GUILayout.Label(kLabelGooglePlayDeveloperConsoleSteps);
            GUILayout.Label(kLabelGooglePasteKeyInstructions);

            if (SettingsWebRequest == null)
            {
                SettingsWebRequest = new SettingsWebRequests(OnGetIAPSettings);
            }

            m_GooglePlayPublicKey = EditorGUILayout.TextArea(
                m_GooglePlayPublicKey,
                GUILayout.MinHeight(20),
                GUILayout.MaxHeight(50));

            GUILayout.Label(kObfuscateKeyInstructions);
            if (!string.IsNullOrEmpty(m_GoogleError))
            {
                GUILayout.Label(m_GoogleError, m_ErrorStyle);
            }

            if (GUILayout.Button(kLabelGenerateGoogle))
            {
                ObfuscateSecrets(includeGoogle: true);
            }

            GUILayout.Label(kDashboardInstructions);

            GUILink(kLabelDashboardLink, GetFormattedDashboardUrl());
        }

        static string GetFormattedDashboardUrl()
        {
            return $"https://dashboard.unity3d.com/organizations/{OrganizationProvider.Organization.Key}/projects/{CloudProjectSettings.projectId}/analytics/v2/dashboards/revenue";
        }

        void ObfuscateSecrets(bool includeGoogle)
        {
            ObfuscationGenerator.ObfuscateSecrets(includeGoogle: includeGoogle,
                googleError: ref m_GoogleError, googlePlayPublicKey: m_GooglePlayPublicKey);
        }
    }
}

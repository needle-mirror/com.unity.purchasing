using System;
using Unity.Services.Core.Editor.OrganizationHandler;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Core.Editor.Environments;

namespace UnityEditor.Purchasing
{
    internal class GooglePlayConfigurationSettingsBlock : IPurchasingSettingsUIBlock
    {
        const string k_GooglePlayConfigItem = "GooglePlayConfigItem";
        const string k_CodaConfigItem = "CodaConfigItem";
        const string k_StripeConfigItem = "StripeConfigItem";
        const string k_PaymentProviderConfigurationTitle = "PaymentProviderConfigurationTitle";
        const string k_UnlinkedSeparator = "UnlinkedSeparator";
        const string k_RevenueValidationBlock = "RevenueValidationBlock";
        const string k_ConfiguredBadge = "GoogleKeyConfiguredBadge";
        const string k_MissingKeyBadge = "GoogleKeyMissingBadge";
        const string k_InfoIcon = "GoogleKeyInfoIcon";
        const string k_UnlinkedFeatures = "UnlinkedFeatures";
        const string k_MissingKeyMode = "missing-key-mode";
        const string k_UnlinkedMissingKeyMode = "unlinked-missing-key-mode";
        const string k_GoToCloudProjectButton = "GoToCloudProjectButton";
        const string k_IgnoreWarningToggle = "IgnoreGoogleLicenseKeyWarningToggle";
        const string k_GooglePlayKeyEntry = "GooglePlayKeyEntry";
        const string k_PushLicenseKeyButton = "PushLicenseKeyButton";
        const string k_PushError = "push-error";
        const string k_ErrorKeyFormat = "error-request-format";
        const string k_ErrorUnauthorized = "error-unauthorized-user";
        const string k_ErrorServer = "error-server-error";
        const string k_CodaPaymentProviderLink = "CodaPaymentProviderLink";
        const string k_StripePaymentProviderLink = "StripePaymentProviderLink";
        const string k_VisibleClass = "visible";
        const string k_ExpandedClass = "expanded";

        readonly GoogleConfigurationData m_GooglePlayDataRef;
        readonly GoogleConfigurationWebRequests m_WebRequests;
        readonly bool m_IsProjectLinked;

        VisualElement m_ConfigurationBlock;
        readonly GoogleObfuscatorSection m_ObfuscatorSection;

        internal GooglePlayConfigurationSettingsBlock(bool isProjectLinked = true)
        {
            m_IsProjectLinked = isProjectLinked;
            m_GooglePlayDataRef = GoogleConfigService.Instance().GoogleConfigData;
            m_WebRequests = new GoogleConfigurationWebRequests(OnGetGooglePlayKey);

            m_ObfuscatorSection = new GoogleObfuscatorSection(m_GooglePlayDataRef);
        }

        public VisualElement GetUIBlockElement()
        {
            return SetupConfigurationBlock();
        }

        VisualElement SetupConfigurationBlock()
        {
            // The block outlives a relink, so the cache is checked each time the page is built, not only once.
            m_GooglePlayDataRef.ResetIfCloudProjectChanged();
            m_ConfigurationBlock = SettingsUIUtils.CloneUIFromTemplate(UIResourceUtils.googlePlayConfigUxmlPath);

            SetupStyleSheets();
            // The editor's own info icon, so it follows the light/dark skin.
            m_ConfigurationBlock.Q(k_InfoIcon).style.backgroundImage = (Texture2D)EditorGUIUtility.IconContent("console.infoicon.sml").image;
            PopulateConfigBlock();
            PopulateObfuscatorBlock();

            if (m_IsProjectLinked)
            {
                SetDisplayed(m_ConfigurationBlock.Q(k_UnlinkedFeatures), false);
                m_ConfigurationBlock.Q(k_UnlinkedSeparator)?.RemoveFromHierarchy();
                ObtainExistingGooglePlayKey();
            }
            else
            {
                ShowGoogleCardOnly();
            }

            return m_ConfigurationBlock;
        }

        void SetupStyleSheets()
        {
            m_ConfigurationBlock.AddStyleSheetPath(UIResourceUtils.purchasingCommonUssPath);
            m_ConfigurationBlock.AddStyleSheetPath(EditorGUIUtility.isProSkin ? UIResourceUtils.purchasingDarkUssPath : UIResourceUtils.purchasingLightUssPath);
        }

        void PopulateConfigBlock()
        {
            ToggleGoogleKeyStateDisplay();
            SetupLinkActions();
            SetupFoldOutActions();
            SetupIgnoreWarningToggle();
        }

        // Without a cloud project there is no payment provider to connect and no dashboard key to show.
        void ShowGoogleCardOnly()
        {
            SetDisplayed(m_ConfigurationBlock.Q(k_PaymentProviderConfigurationTitle), false);
            SetDisplayed(m_ConfigurationBlock.Q(k_CodaConfigItem), false);
            SetDisplayed(m_ConfigurationBlock.Q(k_StripeConfigItem), false);
            SetDisplayed(m_ConfigurationBlock.Q(k_RevenueValidationBlock), false);
            SetDisplayed(m_ConfigurationBlock.Q(k_PushLicenseKeyButton), false);
            m_ConfigurationBlock.Q(k_GooglePlayConfigItem)?.AddToClassList(k_ExpandedClass);
        }

        void SetupFoldOutActions() {
            List<VisualElement> clickableHeaders = m_ConfigurationBlock.Query(className: "configuration-item-header").ToList();

            foreach (VisualElement element in clickableHeaders)
            {
                if (element != null)
                {
                    // Register a callback for the clicked event
                    element.RegisterCallback<ClickEvent>(onFoldOutAction);
                }
            }
        }

        void onFoldOutAction(ClickEvent evt) {
            VisualElement currentHandler = evt.currentTarget as VisualElement;
            currentHandler.parent.ToggleInClassList(k_ExpandedClass);
        }

        void SetupIgnoreWarningToggle()
        {
            var toggle = m_ConfigurationBlock.Q<Toggle>(k_IgnoreWarningToggle);
            toggle.SetValueWithoutNotify(IapEditorProjectSettings.IgnoreGoogleLicenseKeyWarning);
            toggle.RegisterValueChangedCallback(evt =>
            {
                IapEditorProjectSettings.IgnoreGoogleLicenseKeyWarning = evt.newValue;
                ToggleGoogleKeyStateDisplay();
            });
        }

        void PopulateObfuscatorBlock()
        {
            m_ObfuscatorSection.SetupObfuscatorBlock(m_ConfigurationBlock);
            m_ObfuscatorSection.RegisterGooglePlayKeyChangedCallback();

            // Edits that were never pushed don't survive a reopen: a linked project shows its cloud key again.
            if (m_IsProjectLinked && !string.IsNullOrEmpty(m_GooglePlayDataRef.cloudGooglePlayKey))
            {
                m_GooglePlayDataRef.googlePlayKey = m_GooglePlayDataRef.cloudGooglePlayKey;
            }

            SetGooglePlayKeyText(m_GooglePlayDataRef.googlePlayKey);
            m_ObfuscatorSection.RegisterGooglePlayKeyChangedCallback(_ => UpdatePushButtonState());
            m_ConfigurationBlock.Q<Button>(k_PushLicenseKeyButton).clicked += OnPushLicenseKeyClicked;
            ShowPushError(null);
        }

        // Always refetch, so the page reflects a key set or changed on the dashboard since it was last opened.
        void ObtainExistingGooglePlayKey()
        {
            m_WebRequests.RequestRetrieveKeyOperation();
        }

        void SetupLinkActions()
        {
            m_ConfigurationBlock.Q<Button>(k_GoToCloudProjectButton).clicked += OpenCloudProjectSettings;

            SetupPaymentProviderLink(k_CodaPaymentProviderLink);
            SetupPaymentProviderLink(k_StripePaymentProviderLink);
        }

        void SetupPaymentProviderLink(string elementName)
        {
            var link = m_ConfigurationBlock.Q(elementName);
            if (link != null)
            {
                var clickable = new Clickable(OpenPaymentProviderUnityDashboard);
                link.AddManipulator(clickable);
            }
        }

        void OpenPaymentProviderUnityDashboard()
        {
            Application.OpenURL(BuildPaymentProviderUri());
        }

        string BuildPaymentProviderUri()
        {
            var environmentId = EnvironmentsApi.Instance.ActiveEnvironmentId;
            if (environmentId == Guid.Empty)
            {
                try
                {
                    environmentId = EnvironmentsApi.Instance.Environments.First(envInfo => envInfo.Name == "production").Id;
                }
                catch (Exception)
                {
                    // ignored
                }

                if (environmentId == Guid.Empty)
                {
                    return string.Format(PurchasingUrls.inAppPurchasesUrl, OrganizationProvider.Organization.Key);
                }
            }

            return string.Format(PurchasingUrls.paymentProviderUrl, OrganizationProvider.Organization.Key, CloudProjectSettings.projectId, environmentId);
        }

        static void OpenCloudProjectSettings()
        {
            Application.OpenURL(BuildCloudProjectSettingsUri());

            GameServicesEventSenderHelpers.SendProjectSettingsOpenDashboardForPublicKey();
        }

        internal static string BuildCloudProjectSettingsUri()
        {
            return string.Format(PurchasingUrls.cloudProjectSettingsUrl, OrganizationProvider.Organization.Key, CloudProjectSettings.projectId);
        }

        void ToggleGoogleKeyStateDisplay()
        {
            var state = m_GooglePlayDataRef.revenueTrackingState;

            m_ConfigurationBlock.Q(k_ConfiguredBadge)?.EnableInClassList(k_VisibleClass, m_IsProjectLinked && state == GooglePlayRevenueTrackingKeyState.Verified);
            // An unlinked project has no cloud key by definition.
            var isKeyMissing = !m_IsProjectLinked || state == GooglePlayRevenueTrackingKeyState.NoKey;
            m_ConfigurationBlock.Q(k_MissingKeyBadge)?.EnableInClassList(k_VisibleClass, isKeyMissing);
            m_ConfigurationBlock.Q(k_InfoIcon)?.EnableInClassList(k_VisibleClass, isKeyMissing);

            // Once ignored, the warning and its checkbox stay hidden. Turning the warning back on means editing
            // ProjectSettings/Packages/com.unity.purchasing/Settings.asset.
            var ignored = IapEditorProjectSettings.IgnoreGoogleLicenseKeyWarning;
            SetDisplayed(m_ConfigurationBlock.Q(k_MissingKeyMode), !ignored && state == GooglePlayRevenueTrackingKeyState.NoKey);
            SetDisplayed(m_ConfigurationBlock.Q(k_UnlinkedMissingKeyMode), !ignored && !m_IsProjectLinked);
            // A verified key means the build never warns, so there is nothing to ignore.
            SetDisplayed(m_ConfigurationBlock.Q(k_IgnoreWarningToggle), !ignored && (!m_IsProjectLinked || state != GooglePlayRevenueTrackingKeyState.Verified));
            SetDisplayed(m_ConfigurationBlock.Q(k_ErrorKeyFormat), state == GooglePlayRevenueTrackingKeyState.InvalidFormat);
            SetDisplayed(m_ConfigurationBlock.Q(k_ErrorUnauthorized), state == GooglePlayRevenueTrackingKeyState.UnauthorizedUser);
            SetDisplayed(m_ConfigurationBlock.Q(k_ErrorServer), state == GooglePlayRevenueTrackingKeyState.ServerError || state == GooglePlayRevenueTrackingKeyState.CantFetch);
        }

        static void SetDisplayed(VisualElement element, bool displayed)
        {
            if (element != null)
            {
                element.style.display = displayed ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void OnGetGooglePlayKey(string key, GooglePlayRevenueTrackingKeyState state)
        {
            m_GooglePlayDataRef.revenueTrackingState = state;

            if (!string.IsNullOrEmpty(key))
            {
                m_GooglePlayDataRef.cloudGooglePlayKey = key;
                m_GooglePlayDataRef.googlePlayKey = key;
                SetGooglePlayKeyText(key);
            }

            ToggleGoogleKeyStateDisplay();
        }

        void SetGooglePlayKeyText(string key)
        {
            m_ObfuscatorSection.SetGooglePlayKeyText(key ?? string.Empty);
            UpdatePushButtonState();
        }

        void UpdatePushButtonState()
        {
            // Pushing the key the cloud project already has would do nothing.
            var key = m_ConfigurationBlock.Q<TextField>(k_GooglePlayKeyEntry).value?.Trim();
            m_ConfigurationBlock.Q<Button>(k_PushLicenseKeyButton).SetEnabled(
                !string.IsNullOrEmpty(key) && key != m_GooglePlayDataRef.cloudGooglePlayKey);
        }

        void OnPushLicenseKeyClicked()
        {
            var key = m_ConfigurationBlock.Q<TextField>(k_GooglePlayKeyEntry).value?.Trim();
            if (!IsBase64(key))
            {
                ShowPushError("The Google Play License Key is invalid. Copy it again from the Google Play Console under \"Monetization setup\".");
                return;
            }

            // The key verifies this project's Google Play purchases, so replacing a working one must be deliberate.
            if (!string.IsNullOrEmpty(m_GooglePlayDataRef.cloudGooglePlayKey) &&
                !EditorUtility.DisplayDialog("Replace Google Play license key",
                    "This cloud project already has a different Google Play license key. Replacing it changes the key used to verify Google Play purchases for this project.",
                    "Replace", "Cancel"))
            {
                return;
            }

            ShowPushError(null);
            m_ConfigurationBlock.Q<Button>(k_PushLicenseKeyButton).SetEnabled(false);
            m_WebRequests.RequestPushKeyOperation(key, OnLicenseKeyPushed);
        }

        void OnLicenseKeyPushed(long responseCode)
        {
            UpdatePushButtonState();

            if (responseCode / 100 == 2)
            {
                m_WebRequests.RequestRetrieveKeyOperation();
            }
            else if (responseCode == 400)
            {
                // iap-settings answers 400 when the key isn't a Base64-encoded RSA public key (isValidGPK).
                ShowPushError("The license key was rejected as invalid. Copy the Base64-encoded RSA public key again from the Google Play Console under \"Monetization setup\".");
            }
            else if (responseCode == 401 || responseCode == 403)
            {
                ShowPushError("You don't have permission to change this cloud project's settings. Check that you're signed in to the editor with an account that can manage this project.");
            }
            else if (responseCode == GoogleConfigurationWebRequests.k_PushCancelledProjectChanged)
            {
                ShowPushError("The cloud project changed before the license key was sent, so nothing was pushed. Check the key and push again.");
            }
            else if (responseCode == 0)
            {
                ShowPushError("Couldn't reach Unity services. Check your internet connection and try again.");
            }
            else
            {
                ShowPushError($"Pushing the license key to the cloud project failed (HTTP {responseCode}). Please try again later.");
            }
        }

        void ShowPushError(string message)
        {
            var pushError = m_ConfigurationBlock.Q(k_PushError);
            pushError.Q<HelpBox>().text = message;
            SetDisplayed(pushError, !string.IsNullOrEmpty(message));
        }

        static bool IsBase64(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            try
            {
                Convert.FromBase64String(value);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}

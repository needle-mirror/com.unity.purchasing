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
        const string k_GooglePlayLink = "GooglePlayLink";
        const string k_DashboardSettingsLink = "DashboardSettingsLink";
        const string k_VerifiedMode = "verified-mode";
        const string k_UnverifiedMode = "unverified-mode";
        const string k_ErrorKeyFormat = "error-request-format";
        const string k_ErrorUnauthorized = "error-unauthorized-user";
        const string k_ErrorServer = "error-server-error";
        const string k_ErrorFetchKey = "error-fetch-key";
        const string k_CodaPaymentProviderLink = "CodaPaymentProviderLink";
        const string k_StripePaymentProviderLink = "StripePaymentProviderLink";

        readonly GoogleConfigurationData m_GooglePlayDataRef;
        readonly GoogleConfigurationWebRequests m_WebRequests;

        VisualElement m_ConfigurationBlock;
        readonly GoogleObfuscatorSection m_ObfuscatorSection;

        internal GooglePlayConfigurationSettingsBlock()
        {
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
            m_ConfigurationBlock = SettingsUIUtils.CloneUIFromTemplate(UIResourceUtils.googlePlayConfigUxmlPath);

            SetupStyleSheets();
            PopulateConfigBlock();
            PopulateObfuscatorBlock();
            ObtainExistingGooglePlayKey();

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
            currentHandler.parent.ToggleInClassList("expanded");
        }

        void PopulateObfuscatorBlock()
        {
            m_ObfuscatorSection.SetupObfuscatorBlock(m_ConfigurationBlock);
            m_ObfuscatorSection.RegisterGooglePlayKeyChangedCallback();
        }

        void ObtainExistingGooglePlayKey()
        {
            if (m_GooglePlayDataRef.revenueTrackingState != GooglePlayRevenueTrackingKeyState.Verified)
            {
                m_WebRequests.RequestRetrieveKeyOperation();
            }
            else
            {
                SetGooglePlayKeyText(m_GooglePlayDataRef.googlePlayKey);
                ToggleGoogleKeyStateDisplay();
            }
        }

        void SetupLinkActions()
        {
            var googlePlayExternalLink = m_ConfigurationBlock.Q(k_GooglePlayLink);
            if (googlePlayExternalLink != null)
            {
                var clickable = new Clickable(OpenGooglePlayDevConsole);
                googlePlayExternalLink.AddManipulator(clickable);
            }

            var projectSettingsDashboardLink = m_ConfigurationBlock.Q(k_DashboardSettingsLink);
            if (projectSettingsDashboardLink != null)
            {
                var clickable = new Clickable(OpenProjectSettingsUnityDashboard);
                projectSettingsDashboardLink.AddManipulator(clickable);
            }
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

        static void OpenGooglePlayDevConsole()
        {
            Application.OpenURL(PurchasingUrls.googlePlayDevConsoleUrl);
        }

        static void OpenProjectSettingsUnityDashboard()
        {
            Application.OpenURL(BuildProjectSettingsUri());

            GameServicesEventSenderHelpers.SendProjectSettingsOpenDashboardForPublicKey();
        }

        static string BuildProjectSettingsUri()
        {
            return string.Format(PurchasingUrls.protjectSettingUrl, OrganizationProvider.Organization.Key, CloudProjectSettings.projectId);
        }

        void ToggleGoogleKeyStateDisplay()
        {
            ToggleVerifiedModeDisplay();
            ToggleUnverifiedModeDisplay();
        }

        GooglePlayRevenueTrackingKeyState GetTrackingKeyState()
        {
            return m_GooglePlayDataRef.revenueTrackingState;
        }

        void ToggleVerifiedModeDisplay()
        {
            var verifiedMode = m_ConfigurationBlock.Q(k_VerifiedMode);
            if (verifiedMode != null)
            {
                verifiedMode.style.display = GetTrackingKeyState() == GooglePlayRevenueTrackingKeyState.Verified ? DisplayStyle.Flex : (StyleEnum<DisplayStyle>)DisplayStyle.None;
            }
        }

        void ToggleUnverifiedModeDisplay()
        {
            var unVerifiedMode = m_ConfigurationBlock.Q(k_UnverifiedMode);
            if (unVerifiedMode != null)
            {
                unVerifiedMode.style.display = (GetTrackingKeyState() == GooglePlayRevenueTrackingKeyState.Verified)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;

                ToggleErrorStateBlockVisibility(GooglePlayRevenueTrackingKeyState.InvalidFormat, k_ErrorKeyFormat);
                ToggleErrorStateBlockVisibility(GooglePlayRevenueTrackingKeyState.UnauthorizedUser, k_ErrorUnauthorized);
                ToggleErrorStateBlockVisibility(GooglePlayRevenueTrackingKeyState.ServerError, k_ErrorServer);
                ToggleErrorStateBlockVisibility(GooglePlayRevenueTrackingKeyState.CantFetch, k_ErrorFetchKey);
            }
        }

        void ToggleErrorStateBlockVisibility(GooglePlayRevenueTrackingKeyState matchingBlockState, string blockName)
        {
            var errorSection = m_ConfigurationBlock.Q(blockName);
            if (errorSection != null)
            {
                errorSection.style.display = (GetTrackingKeyState() == matchingBlockState)
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }
        }

        void OnGetGooglePlayKey(string key, GooglePlayRevenueTrackingKeyState state)
        {
            m_GooglePlayDataRef.googlePlayKey = key;
            m_GooglePlayDataRef.revenueTrackingState = state;

            if (!string.IsNullOrEmpty(key))
            {
                SetGooglePlayKeyText(key);
            }

            ToggleGoogleKeyStateDisplay();
        }

        void SetGooglePlayKeyText(string key)
        {
            m_ObfuscatorSection.SetGooglePlayKeyText(key);
        }
    }
}

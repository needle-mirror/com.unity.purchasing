using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    internal class GooglePlayConfigurationSettingsBlock : IPurchasingSettingsUIBlock
    {
        const string k_UpdateGooglePlayKeyBtn = "UpdateGooglePlayKeyBtn";
        const string k_GooglePlayLink = "GooglePlayLink";
        const string k_GooglePlayKeyEntry = "GooglePlayKeyEntry";

        const string k_VerifiedMode = "verified-mode";
        const string k_UnverifiedMode = "unverified-mode";
        const string k_ErrorKeyFormat = "error-key-format";
        const string k_ErrorUnauthorized = "error-unauthorized-user";
        const string k_ErrorServer = "error-server-error";
        const string k_ErrorFetchKey = "error-fetch-key";

        const string k_GooglePlayKeyBtnUpdateLabel = "Update";
        const string k_GooglePlayKeyBtnVerifyLabel = "Verify";
        readonly GoogleConfigurationData m_GooglePlayDataRef;
        readonly GoogleConfigurationWebRequests m_WebRequests;

        VisualElement m_ConfigurationBlock;
        readonly GoogleObfuscatorSection m_ObfuscatorSection;

        internal GooglePlayConfigurationSettingsBlock()
        {
            m_GooglePlayDataRef = GoogleConfigService.Instance().GoogleConfigData;
            m_WebRequests = new GoogleConfigurationWebRequests(m_GooglePlayDataRef, OnGetGooglePlayKey, OnUpdateGooglePlayKey);

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
            m_ObfuscatorSection.SetupObfuscatorBlock(m_ConfigurationBlock);

            return m_ConfigurationBlock;
        }

        void SetupStyleSheets()
        {
            m_ConfigurationBlock.AddStyleSheetPath(UIResourceUtils.purchasingCommonUssPath);
            m_ConfigurationBlock.AddStyleSheetPath(EditorGUIUtility.isProSkin ? UIResourceUtils.purchasingDarkUssPath : UIResourceUtils.purchasingLightUssPath);
        }

        void PopulateConfigBlock()
        {
            ObtainExistingGooglePlayKey();
            ToggleGoogleKeyStateDisplay();
            SetupButtonActions();
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

        void SetupButtonActions()
        {
            m_ConfigurationBlock.Q<Button>(k_UpdateGooglePlayKeyBtn).clicked += UpdateGooglePlayKey;
            var googlePlayExternalLink = m_ConfigurationBlock.Q(k_GooglePlayLink);
            if (googlePlayExternalLink != null)
            {
                var clickable = new Clickable(OpenGooglePlayDevConsole);
                googlePlayExternalLink.AddManipulator(clickable);
            }

            m_ConfigurationBlock.Q<TextField>(k_GooglePlayKeyEntry).RegisterValueChangedCallback(evt =>
            {
                m_GooglePlayDataRef.googlePlayKey = evt.newValue;
            });
        }

        void UpdateGooglePlayKey()
        {
            m_WebRequests.RequestUpdateOperation();
        }

        void OpenGooglePlayDevConsole()
        {
            Application.OpenURL(PurchasingUrls.googlePlayDevConsoleUrl);
        }

        void ToggleGoogleKeyStateDisplay()
        {
            ToggleUpdateButtonDisplay();
            ToggleVerifiedModeDisplay();
            ToggleUnverifiedModeDisplay();
        }

        void ToggleUpdateButtonDisplay()
        {
            var updateGooglePlayKeyBtn = m_ConfigurationBlock.Q<Button>(k_UpdateGooglePlayKeyBtn);
            if (updateGooglePlayKeyBtn != null)
            {
                updateGooglePlayKeyBtn.text = GetTrackingKeyState() == GooglePlayRevenueTrackingKeyState.Verified
                    ? k_GooglePlayKeyBtnUpdateLabel
                    : k_GooglePlayKeyBtnVerifyLabel;
            }
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

        void OnGetGooglePlayKey(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                m_GooglePlayDataRef.revenueTrackingState = GooglePlayRevenueTrackingKeyState.Verified;
                SetGooglePlayKeyText(key);
            }
            else
            {
                m_GooglePlayDataRef.revenueTrackingState = GooglePlayRevenueTrackingKeyState.CantFetch;
            }

            ToggleGoogleKeyStateDisplay();
        }

        void SetGooglePlayKeyText(string key)
        {
            m_ConfigurationBlock.Q<TextField>(k_GooglePlayKeyEntry).SetValueWithoutNotify(key);
        }

        void OnUpdateGooglePlayKey(GooglePlayRevenueTrackingKeyState keyState)
        {
            m_GooglePlayDataRef.revenueTrackingState = keyState;

            GameServicesEventSenderHelpers.SendProjectSettingsValidatePublicKey();

            ToggleGoogleKeyStateDisplay();
        }
    }
}

using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    class XboxConfigurationSettingsBlock : IPurchasingSettingsUIBlock
    {
        readonly VisualElement m_XboxConfigurationBlock;
        VisualElement m_ConfigurationBlock;

        public VisualElement GetUIBlockElement()
        {
            return SetupConfigBlock();
        }

        VisualElement SetupConfigBlock()
        {
            m_ConfigurationBlock = SettingsUIUtils.CloneUIFromTemplate(UIResourceUtils.xboxConfigUxmlPath);
            m_ConfigurationBlock.Bind(new SerializedObject(XboxSettingsLoader.LoadOrCreate()));
            SetupStyleSheets();
            SetupPurchaseValidation();

            return m_ConfigurationBlock;
        }

        void SetupPurchaseValidation()
        {
            var toggle = m_ConfigurationBlock.Q<Toggle>("PurchaseValidationToggle");
            var fields = new[]
            {
                m_ConfigurationBlock.Q<TextField>("CloudCodeModuleNameField"),
                m_ConfigurationBlock.Q<TextField>("ServiceTicketFunctionNameField"),
                m_ConfigurationBlock.Q<TextField>("ValidatePurchaseFunctionNameField")
            };

#if IAP_CLOUDCODE_ENABLED
            m_ConfigurationBlock.Q<Label>("CloudCodeRequiredLabel").style.display = DisplayStyle.None;
            SetFieldsEnabled(fields, toggle.value);
            toggle.RegisterValueChangedCallback(evt => SetFieldsEnabled(fields, evt.newValue));
#else
            m_ConfigurationBlock.Q<Label>("CloudCodeRequiredLabel").style.display = DisplayStyle.Flex;
            SetFieldsEnabled(fields, false);
            toggle.SetEnabled(false);
#endif
        }

        static void SetFieldsEnabled(TextField[] fields, bool enabled)
        {
            foreach (var field in fields)
            {
                field.SetEnabled(enabled);
            }
        }

        void SetupStyleSheets()
        {
            m_ConfigurationBlock.AddStyleSheetPath(UIResourceUtils.purchasingCommonUssPath);
            m_ConfigurationBlock.AddStyleSheetPath(EditorGUIUtility.isProSkin ? UIResourceUtils.purchasingDarkUssPath : UIResourceUtils.purchasingLightUssPath);
        }
    }
}

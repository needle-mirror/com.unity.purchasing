using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    class AppleConfigurationSettingsBlock : IPurchasingSettingsUIBlock
    {
        readonly VisualElement m_AppleConfigurationBlock;
        readonly AppleObfuscatorSection m_ObfuscatorSection;
        VisualElement m_ConfigurationBlock;
        readonly string m_AppleErrorMessage;
        readonly string m_GoogleErrorMessage;

        internal AppleConfigurationSettingsBlock()
        {
            m_ObfuscatorSection = new AppleObfuscatorSection();
        }

        public VisualElement GetUIBlockElement()
        {
            return SetupConfigBlock();
        }

        VisualElement SetupConfigBlock()
        {
            m_ConfigurationBlock = SettingsUIUtils.CloneUIFromTemplate(UIResourceUtils.appleConfigUxmlPath);

            m_ObfuscatorSection.SetupObfuscatorBlock(m_ConfigurationBlock);
            SetupStyleSheets();

            return m_ConfigurationBlock;
        }

        void SetupStyleSheets()
        {
            m_ConfigurationBlock.AddStyleSheetPath(UIResourceUtils.purchasingCommonUssPath);
            m_ConfigurationBlock.AddStyleSheetPath(EditorGUIUtility.isProSkin ? UIResourceUtils.purchasingDarkUssPath : UIResourceUtils.purchasingLightUssPath);
        }
    }
}

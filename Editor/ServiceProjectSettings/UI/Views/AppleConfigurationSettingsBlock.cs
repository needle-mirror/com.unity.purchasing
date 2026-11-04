using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    class AppleConfigurationSettingsBlock : IPurchasingSettingsUIBlock
    {
        readonly AppleObfuscatorSection m_ObfuscatorSection;
        VisualElement m_ConfigurationBlock;

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
            StoreCardUtils.SetupCard(m_ConfigurationBlock, "AppleIcon", Color.black, "BuildSettings.iPhone");

            return m_ConfigurationBlock;
        }

        void SetupStyleSheets()
        {
            m_ConfigurationBlock.AddStyleSheetPath(UIResourceUtils.purchasingCommonUssPath);
            m_ConfigurationBlock.AddStyleSheetPath(EditorGUIUtility.isProSkin ? UIResourceUtils.purchasingDarkUssPath : UIResourceUtils.purchasingLightUssPath);
        }
    }

    static class StoreCardUtils
    {
        const string k_ExpandedClass = "expanded";

        internal static void SetupCard(VisualElement block, string iconName, Color logoColor, params string[] builtinIconNames)
        {
            var icon = block.Q<Image>(iconName);
            // The pro-skin ("d_") variant is white, so tinting it gives the exact logo colour on the white tile in both skins.
            foreach (var builtinIconName in builtinIconNames)
            {
                icon.image = EditorGUIUtility.FindTexture("d_" + builtinIconName);
                if (icon.image != null)
                {
                    break;
                }
            }
            icon.tintColor = logoColor;

            block.Query(className: "configuration-item-header").ForEach(header =>
                header.RegisterCallback<ClickEvent>(_ => header.parent.ToggleInClassList(k_ExpandedClass)));
        }
    }
}

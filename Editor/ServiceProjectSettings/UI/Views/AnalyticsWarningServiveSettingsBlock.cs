using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    internal class AnalyticsWarningSettingsBlock : IPurchasingSettingsUIBlock
    {
        VisualElement m_CatalogBlock;

        public VisualElement GetUIBlockElement()
        {
            m_CatalogBlock = SettingsUIUtils.CloneUIFromTemplate(UIResourceUtils.analyticsWarningUxmlPath);
            SetupStyleSheets();
            return m_CatalogBlock;
        }

        void SetupStyleSheets()
        {
            m_CatalogBlock.AddStyleSheetPath(UIResourceUtils.purchasingCommonUssPath);
            m_CatalogBlock.AddStyleSheetPath(EditorGUIUtility.isProSkin ? UIResourceUtils.purchasingDarkUssPath : UIResourceUtils.purchasingLightUssPath);
        }
    }
}

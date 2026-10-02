using UnityEditor.Purchasing.Editor.Authoring.Import.Legacy;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// One-button migration from the codeless <c>IAPProductCatalog.json</c> to a Remote Catalog CSV.
    /// Only added to the settings page when the project actually has a legacy catalog.
    /// </summary>
    internal class LegacyCatalogMigrationSettingsBlock : IPurchasingSettingsUIBlock
    {
        const string k_MigrateBtn = "CatalogEditorButton";

        VisualElement m_CatalogBlock;

        public VisualElement GetUIBlockElement()
        {
            // Asked each time the page is built, so navigating away and back picks up a catalog
            // that has appeared or gone since.
            if (!LegacyCatalogMigration.HasLegacyCatalog())
            {
                return new VisualElement();
            }

            m_CatalogBlock = SettingsUIUtils.CloneUIFromTemplate(UIResourceUtils.catalogUxmlPath);

            m_CatalogBlock.AddStyleSheetPath(UIResourceUtils.purchasingCommonUssPath);
            m_CatalogBlock.AddStyleSheetPath(EditorGUIUtility.isProSkin
                ? UIResourceUtils.purchasingDarkUssPath
                : UIResourceUtils.purchasingLightUssPath);

            m_CatalogBlock.Q<Button>(k_MigrateBtn).clicked += Migrate;

            return m_CatalogBlock;
        }

        static void Migrate()
        {
            var path = LegacyCatalogMigration.Migrate();
            if (path == null)
            {
                Debug.unityLogger.LogIAPWarning("No products found in the legacy catalog, so nothing was migrated.");
                return;
            }

            Debug.unityLogger.LogIAP($"Migrated the legacy catalog to {path}. " +
                "Review it, then deploy it from the Deployment Window.");
            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(path));
        }
    }
}

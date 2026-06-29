using System;
using System.Collections.Generic;
using UnityEditor.Purchasing.Editor.Authoring.Core.IO;
using UnityEditor.Purchasing.Editor.Authoring.Import.Apple;
using UnityEditor.Purchasing.Editor.Authoring.Import.Google.ApiFetcher;
using UnityEditor.Purchasing.Editor.Authoring.PurchasingAdminApi;
using Unity.Purchasing.Editor.Shared.WebApi.Network;
using Unity.Services.Core.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Editor.Authoring.Import.UI
{
    /// <summary>
    /// Thin shell editor window for importing in-app product catalogs from any provider.
    ///
    /// Provider selection is done via tabs inside the window.
    /// Providers are supplied externally via <see cref="Open(System.Collections.Generic.IReadOnlyList{CatalogImportProvider})"/>,
    /// making the window fully testable and extensible.
    /// Switching tabs while an import is in progress prompts the user before clearing data.
    /// </summary>
    class CatalogImportWindow : EditorWindow
    {
        const string k_MenuItemRoot = "Services/In-App Purchasing";
        const string k_CatalogImportMenuPath = k_MenuItemRoot + "/Import Catalog";

        const string k_WindowTitle = "Catalog Import";

        const string k_UxmlPath = "Packages/com.unity.purchasing/Editor/Authoring/Import/UI/Assets/CatalogImportWindow.uxml";
        const string k_UssPath = "Packages/com.unity.purchasing/Editor/Authoring/Import/UI/Assets/CatalogImportWindow.uss";

        static IReadOnlyList<CatalogImportProvider> s_PendingProviders;

        [SerializeField]
        int m_SelectedTab;

        CatalogImportState m_State;
        CatalogImportController m_Controller;
        CatalogImportProviderPanelView m_LeftPanel;
        CatalogImportResultsPanelView m_RightPanel;

        /// <summary>
        /// Opens the Catalog Import window with the supplied providers.
        /// Each provider becomes a tab in the order given.
        /// </summary>
        public static CatalogImportWindow Open(IReadOnlyList<CatalogImportProvider> providers)
        {
            if (providers == null || providers.Count == 0)
            {
                throw new ArgumentException("At least one provider must be supplied.", nameof(providers));
            }

            s_PendingProviders = providers;
            var window = GetWindow<CatalogImportWindow>(k_WindowTitle);
            return window;
        }

        /// <summary>
        /// Creates the default set of providers used by the menu item.
        /// </summary>
        public static List<CatalogImportProvider> CreateDefaultProviders()
        {
            var providers = new List<CatalogImportProvider>();
            var tokenProvider = new AccessTokens();

            {
                var apiClient = new ApiClient(null);
                var platformCatalogApi = new PlatformCatalogApi(apiClient);
                var fetcher = new AppleCatalogApiFetcher(platformCatalogApi, tokenProvider);
                providers.Add(new CatalogImportProvider(
                    "App Store",
                    fetcher,
                    new AppleCatalogFetcherConfigDrawer(fetcher),
                    iconCssClass: "tab-icon--app-store"));
            }

            {
                var apiClient = new ApiClient(null);
                var platformCatalogApi = new PlatformCatalogApi(apiClient);
                var fetcher = new GoogleCatalogApiFetcher(platformCatalogApi, tokenProvider);
                providers.Add(new CatalogImportProvider(
                    "Google Play Store",
                    fetcher,
                    new GoogleCatalogFetcherConfigDrawer(fetcher),
                    iconCssClass: "tab-icon--google-play"));
            }

            return providers;
        }

        [MenuItem(k_CatalogImportMenuPath, false, 200)]
        public static void ShowWindow()
        {
            Open(CreateDefaultProviders());
        }

        void CreateGUI()
        {
            // Consume pending providers or fall back to defaults.
            List<CatalogImportProvider> providers;
            if (s_PendingProviders != null)
            {
                providers = new List<CatalogImportProvider>(s_PendingProviders);
                s_PendingProviders = null;
            }
            else
            {
                providers = CreateDefaultProviders();
            }

            minSize = new Vector2(660, 400);

            // Load UXML & USS
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_UxmlPath);
            if (tree != null)
            {
                tree.CloneTree(rootVisualElement);
            }

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(k_UssPath);
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            // Wire up MVC
            m_State = new CatalogImportState();
            var csvParser = PurchasingAuthoringServiceProvider.GetService<ICatalogCsvParser>();
            m_Controller = new CatalogImportController(m_State, providers, csvParser);
            m_Controller.Initialize(Mathf.Clamp(m_SelectedTab, 0, providers.Count - 1));

            m_LeftPanel = new CatalogImportProviderPanelView(rootVisualElement, m_State, m_Controller);
            m_LeftPanel.Initialize();
            m_LeftPanel.RefreshProviderConfigUI(m_Controller.ActiveProvider);

            m_RightPanel = new CatalogImportResultsPanelView(rootVisualElement, m_State);
            m_RightPanel.Initialize();
        }

        void OnDisable()
        {
            m_LeftPanel?.Dispose();
            m_Controller?.Dispose();
        }
    }
}

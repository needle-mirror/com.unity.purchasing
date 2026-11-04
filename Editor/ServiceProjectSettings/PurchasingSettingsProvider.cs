#if SERVICES_SDK_CORE_ENABLED
using System.Collections.Generic;
using Unity.Services.Core.Editor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    internal class PurchasingSettingsProvider : EditorGameServiceSettingsProvider
    {
        const string k_ProjectSettingsName = "In-App Purchasing";

        const string k_Title = "In-App Purchasing";
        const string k_Description = "Simplify cross platform In-App Purchasing";
        const string k_UnlinkedContentName = "UnlinkedIapContent";
        const string k_UnlinkedTabsClass = "iap-unlinked-tabs";
        const string k_UnlinkedSetupTabClass = "iap-unlinked-setup-tab";
        const string k_UnlinkedFeaturesHostClass = "iap-unlinked-features";
        const string k_UnlinkedSeparatorName = "UnlinkedSeparator";
        const string k_UnlinkedFeaturesName = "UnlinkedFeatures";

        const string k_CloudSetupTabTitle = "Cloud setup";
        const string k_SetupTabTitle = "Setup";
        const string k_AISkillsTabTitle = "AI Skills";
        const string k_TabBarClass = "iap-tab-bar";
        const string k_TabClass = "iap-tab";
        const string k_SelectedTabClass = "iap-tab--selected";
        const string k_UnavailableTabClass = "iap-tab--unavailable";
        const string k_ServiceOffTooltip = "Turn on In-App Purchasing to set up stores and cloud services.";
        // Marks Core's toggle once our handler is on it, since the unlinked content is rebuilt on every switch.
        const string k_ServiceToggleWiredClass = "iap-service-toggle-wired";
        const string k_TabContentClass = "iap-tab-content";

        const string k_GoToRemoteCatalogButtonName = "GoToRemoteCatalogButton";
        const string k_GoToPaymentProvidersButtonName = "GoToPaymentProvidersButton";
        const string k_CloudBlocksName = "CloudBlocks";
        const string k_NotRequiredForCloudSectionName = "NotRequiredForCloudSection";
        const string k_NotRequiredForCloudBlocksName = "NotRequiredForCloudBlocks";
        const string k_TrailingBlocksName = "TrailingBlocks";

        // Element names from Core's ProjectSettingsCommonHeader.uxml.
        const string k_CoreHeaderClass = "project-settings-header-container";
        const string k_CoreDescriptionName = "service-description";
        const string k_CoreDashboardButtonContainerName = "dashboard-button-container";
        const string k_CoreServiceToggleName = "service-toggle";
        const string k_SeparatorClass = "separator";

        static int s_SelectedTab;
        readonly PurchasingGameService m_Service;
        bool m_CallbacksInitialized;
        ScrollView m_ScrollView;
        bool m_ServiceOnWhileUnlinked;
        bool m_RestoreServiceAfterLink;

        SimpleStateMachine<bool> m_StateMachine;
        PurchasingDisabledState m_DisabledState;
        PurchasingEnabledState m_EnabledState;

        internal VisualElement rootVisualElement { get; private set; }

        [SettingsProvider]
        internal static SettingsProvider CreateServicesProvider()
        {
#if ENABLE_EDITOR_GAME_SERVICES
            return new PurchasingSettingsProvider(SettingsScope.Project, PurchasingSettingsKeywords.GetKeywords());
#else
            return null;
#endif
        }

        internal PurchasingSettingsProvider(SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(GetSettingsPath(), scopes, keywords)
        {
            m_Service = (PurchasingGameService)EditorGameServiceRegistry.Instance.GetEditorGameService<PurchasingServiceIdentifier>();

            ConfigureStateMachine();
        }

        void ConfigureStateMachine()
        {
            m_StateMachine = new SimpleStateMachine<bool>();

            m_StateMachine.AddEvent(false);
            m_StateMachine.AddEvent(true);

            m_DisabledState = new PurchasingDisabledState(m_StateMachine);
            m_EnabledState = new PurchasingEnabledState(m_StateMachine);

            m_StateMachine.AddState(m_DisabledState);
            m_StateMachine.AddState(m_EnabledState);
        }

        internal static string GetSettingsPath()
        {
            return GenerateProjectSettingsPath(k_ProjectSettingsName);
        }

        protected override IEditorGameService EditorGameService => m_Service;

        protected override string Title => k_Title;

        protected override string Description => k_Description;

        protected override VisualElement GenerateServiceDetailUI()
        {
            rootVisualElement = new VisualElement();
            rootVisualElement.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                HideCoreHeaderDetails(rootVisualElement);
                CancelRestoreWhenToggled(rootVisualElement);
            });
            // Core swaps this view for its linking UI when the project is unlinked while the window is open.
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(_ => m_ScrollView?.schedule.Execute(() => AddUnlinkedContent(m_ScrollView)));

            InitializeStateMachine();
            RefreshDetailUI();

            return rootVisualElement;
        }

        void RefreshDetailUI()
        {
            // An unlinked project never builds the detail UI; Core draws its linking UI instead.
            if (rootVisualElement == null)
            {
                return;
            }

            rootVisualElement.Clear();
            SetupStyleSheets(rootVisualElement);

            var uiState = (BasePurchasingState)m_StateMachine.currentState;

            rootVisualElement.Add(uiState.editorSetupBlock.GetUIBlockElement());
            rootVisualElement.Add(CreateTabs(
                (k_CloudSetupTabTitle, CreateCloudSetupTabContent(uiState)),
                (k_AISkillsTabTitle, new AISkillsSettingsBlock().GetUIBlockElement())));
        }

        static VisualElement CreateCloudSetupTabContent(BasePurchasingState uiState)
        {
            var content = SettingsUIUtils.CloneUIFromTemplate(UIResourceUtils.purchasingServicesRootUxmlPath) ?? new VisualElement();

            SetupDashboardButton(content.Q<Button>(k_GoToRemoteCatalogButtonName), PurchasingUrls.remoteCatalogUrl);
            SetupDashboardButton(content.Q<Button>(k_GoToPaymentProvidersButtonName), PurchasingUrls.paymentProviderUrl);

            AddBlocks(content.Q(k_CloudBlocksName), uiState.cloudBlocks);
            AddBlocks(content.Q(k_NotRequiredForCloudBlocksName), uiState.notRequiredForCloudBlocks);
            AddBlocks(content.Q(k_TrailingBlocksName), uiState.trailingBlocks);

            SetDisplayed(content.Q(k_NotRequiredForCloudSectionName), uiState.notRequiredForCloudBlocks.Count > 0);

            return content;
        }

        static void SetupDashboardButton(Button button, string environmentPageUrlFormat)
        {
            if (button != null)
            {
                button.clicked += () => Application.OpenURL(PurchasingUrls.BuildEnvironmentPageUrl(environmentPageUrlFormat));
            }
        }

        static void AddBlocks(VisualElement host, IEnumerable<IPurchasingSettingsUIBlock> blocks)
        {
            if (host == null)
            {
                return;
            }

            foreach (var block in blocks)
            {
                host.Add(block.GetUIBlockElement());
            }
        }

        // The Editor setup sheets are shared too, so tab content (AI skills rows) can reuse the row and label classes.
        static void SetupStyleSheets(VisualElement element)
        {
            element.AddStyleSheetPath(UIResourceUtils.purchasingCommonUssPath);
            element.AddStyleSheetPath(EditorGUIUtility.isProSkin ? UIResourceUtils.purchasingDarkUssPath : UIResourceUtils.purchasingLightUssPath);
            element.AddStyleSheetPath(UIResourceUtils.platformSupportCommonUssPath);
            element.AddStyleSheetPath(EditorGUIUtility.isProSkin ? UIResourceUtils.platformSupportDarkUssPath : UIResourceUtils.platformSupportLightUssPath);
        }

        // Built from plain elements because TabView needs 2023.2. Every tab header switches on click, and the
        // selected tab is shared by the linked and unlinked pages so it survives Core rebuilding the view.
        // A tab without content is shown but can't be opened.
        static VisualElement CreateTabs(params (string title, VisualElement content)[] tabs)
        {
            var root = new VisualElement();
            var bar = new VisualElement();
            bar.AddToClassList(k_TabBarClass);
            root.Add(bar);

            var headers = new List<Label>();
            for (var i = 0; i < tabs.Length; i++)
            {
                var index = i;
                var header = new Label(tabs[i].title);
                header.AddToClassList(k_TabClass);
                headers.Add(header);
                bar.Add(header);

                if (tabs[i].content == null)
                {
                    // Not SetEnabled(false): a disabled element gets no tooltip.
                    header.AddToClassList(k_UnavailableTabClass);
                    header.tooltip = k_ServiceOffTooltip;
                    continue;
                }

                header.RegisterCallback<ClickEvent>(_ =>
                {
                    s_SelectedTab = index;
                    Show(index);
                });
                tabs[i].content.AddToClassList(k_TabContentClass);
                root.Add(tabs[i].content);
            }

            // Falls back to the first tab that can be opened, without forgetting the user's choice for later.
            var preferred = Mathf.Clamp(s_SelectedTab, 0, tabs.Length - 1);
            Show(tabs[preferred].content != null ? preferred : System.Array.FindIndex(tabs, tab => tab.content != null));
            return root;

            void Show(int selected)
            {
                for (var i = 0; i < tabs.Length; i++)
                {
                    headers[i].EnableInClassList(k_SelectedTabClass, i == selected);
                    if (tabs[i].content != null)
                    {
                        tabs[i].content.style.display = i == selected ? DisplayStyle.Flex : DisplayStyle.None;
                    }
                }
            }
        }

        // The page has its own header, so Core's description, dashboard link and separator would only repeat the title.
        static void HideCoreHeaderDetails(VisualElement detailUI)
        {
            var scrollView = detailUI.GetFirstAncestorOfType<ScrollView>();
            var header = scrollView?.Q(className: k_CoreHeaderClass);
            if (header == null)
            {
                return;
            }

            SetDisplayed(header.Q(k_CoreDescriptionName), false);
            SetDisplayed(header.Q(k_CoreDashboardButtonContainerName), false);

            var headerTemplate = header.parent;
            var separatorIndex = headerTemplate.IndexOf(header) + 1;
            if (separatorIndex < headerTemplate.childCount && headerTemplate[separatorIndex].ClassListContains(k_SeparatorClass))
            {
                SetDisplayed(headerTemplate[separatorIndex], false);
            }
        }

        // Flipping Core's toggle by hand cancels restoring IAP after linking. Trickling down runs this before
        // Core's own toggle callback, which is what calls Disable.
        void CancelRestoreWhenToggled(VisualElement detailUI)
        {
            var header = detailUI.GetFirstAncestorOfType<ScrollView>()?.Q(className: k_CoreHeaderClass);
            header?.RegisterCallback<ChangeEvent<bool>>(_ => m_RestoreServiceAfterLink = false, TrickleDown.TrickleDown);
        }

        static void SetDisplayed(VisualElement element, bool displayed)
        {
            if (element != null)
            {
                element.style.display = displayed ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            base.OnActivate(searchContext, rootElement);

            InitializeStateMachine();
            InitializeServiceCallbacks();
            m_ScrollView = rootElement.Q<ScrollView>();
            AddUnlinkedContent(m_ScrollView);
        }

        // Core draws its own linking UI for an unlinked project and never calls GenerateServiceDetailUI,
        // so our content is appended after it. Core clears its view whenever it refreshes, which detaches
        // the content, so it re-appends itself on the next frame while the project is still unlinked.
        void AddUnlinkedContent(ScrollView scrollView)
        {
            if (scrollView == null || CloudProjectSettings.projectBound || scrollView.Q(k_UnlinkedContentName) != null)
            {
                return;
            }

            var content = new VisualElement { name = k_UnlinkedContentName };
            SetupStyleSheets(content);
            var isServiceOn = m_Service.Enabler.IsEnabled();
            m_ServiceOnWhileUnlinked = isServiceOn;
            if (isServiceOn)
            {
                AddUnlinkedSetup(content);
            }
            else
            {
                AddUnlinkedServiceOff(content);
            }

            content.RegisterCallback<DetachFromPanelEvent>(_ => scrollView.schedule.Execute(() => AddUnlinkedContent(scrollView)));
            scrollView.Add(content);
            SetCoreLinkingUIDisplayed(scrollView, content, isServiceOn);
            ShowServiceToggle(scrollView);
        }

        static void AddUnlinkedServiceOff(VisualElement content)
        {
            content.Add(PlatformsAndStoresServiceSettingsBlock.CreateStateSpecificBlock(false).GetUIBlockElement());
            content.Add(CreateTabs(
                (k_CloudSetupTabTitle, null),
                (k_AISkillsTabTitle, new AISkillsSettingsBlock().GetUIBlockElement())));
        }

        static void AddUnlinkedSetup(VisualElement content)
        {
            var googlePlayCard = new GooglePlayConfigurationSettingsBlock(isProjectLinked: false).GetUIBlockElement();
            // The features list ships inside the Google Play block, but belongs above the tabs.
            var features = new VisualElement();
            features.AddToClassList(k_UnlinkedFeaturesHostClass);
            foreach (var name in new[] { k_UnlinkedSeparatorName, k_UnlinkedFeaturesName })
            {
                var element = googlePlayCard.Q(name);
                if (element != null)
                {
                    features.Add(element);
                }
            }
            content.Add(features);

            var setupTab = new VisualElement();
            setupTab.AddToClassList(k_UnlinkedSetupTabClass);
            setupTab.Add(googlePlayCard);
            setupTab.Add(new AppleConfigurationSettingsBlock().GetUIBlockElement());

            var tabs = CreateTabs(
                (k_SetupTabTitle, setupTab),
                (k_AISkillsTabTitle, new AISkillsSettingsBlock().GetUIBlockElement()));
            tabs.AddToClassList(k_UnlinkedTabsClass);
            content.Add(tabs);
        }

        // With In-App Purchasing off there is no cloud project to link for yet, so Core's linking steps, and the
        // description and separator under its title, are hidden like on the linked page.
        static void SetCoreLinkingUIDisplayed(ScrollView scrollView, VisualElement content, bool displayed)
        {
            var header = scrollView.Q(className: k_CoreHeaderClass);
            SetDisplayed(header?.Q(k_CoreDescriptionName), displayed);

            foreach (var child in scrollView.contentContainer.Children())
            {
                if (child == content)
                {
                    continue;
                }

                if (header == null || !child.Contains(header))
                {
                    SetDisplayed(child, displayed);
                    continue;
                }

                var separatorIndex = header.parent.IndexOf(header) + 1;
                if (separatorIndex < header.parent.childCount && header.parent[separatorIndex].ClassListContains(k_SeparatorClass))
                {
                    SetDisplayed(header.parent[separatorIndex], displayed);
                }
            }
        }

        // Core only shows its service toggle for a linked project, but IAP can be enabled without one.
        // Core rebuilds its header along with the unlinked content, so this runs once per header.
        void ShowServiceToggle(ScrollView scrollView)
        {
            var toggle = scrollView.Q<Toggle>(k_CoreServiceToggleName);
            if (toggle == null || toggle.ClassListContains(k_ServiceToggleWiredClass))
            {
                return;
            }

            toggle.AddToClassList(k_ServiceToggleWiredClass);
            toggle.visible = true;
            toggle.SetValueWithoutNotify(m_Service.Enabler.IsEnabled());
            toggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    m_Service.Enabler.Enable();
                }
                else
                {
                    m_Service.Enabler.Disable();
                }

                // Detaching re-adds the content on the next frame, built for the new state.
                scrollView.Q(k_UnlinkedContentName)?.RemoveFromHierarchy();
            });
        }

        void InitializeStateMachine()
        {
            if (m_StateMachine.currentState == null)
            {
                if (m_Service.Enabler.IsEnabled())
                {
                    m_StateMachine.Initialize(m_EnabledState);
                }
                else
                {
                    m_StateMachine.Initialize(m_DisabledState);
                }
            }
        }

        public override void OnDeactivate()
        {
            base.OnDeactivate();

            FinalizeStateMachine();
            FinalizeServiceCallbacks();
        }

        void FinalizeStateMachine()
        {
            m_StateMachine.ClearCurrentState();
        }

        void InitializeServiceCallbacks()
        {
            if (!m_CallbacksInitialized)
            {
                m_Service.AddEnableAction(EnableOperationCompleted);
                m_Service.AddDisableAction(DisableOperationCompleted);
                CloudProjectSettingsEventManager.instance.projectStateChanged += OnProjectStateChanged;

                m_CallbacksInitialized = true;
            }
        }

        void FinalizeServiceCallbacks()
        {
            if (m_CallbacksInitialized)
            {
                m_Service.RemoveEnableAction(EnableOperationCompleted);
                m_Service.RemoveDisableAction(DisableOperationCompleted);
                CloudProjectSettingsEventManager.instance.projectStateChanged -= OnProjectStateChanged;

                m_CallbacksInitialized = false;
            }
        }

        // Linking applies the cloud project's IAP flag, which can turn IAP off. Linking from this page with IAP on
        // restores it when that happens, as if it were turned on after linking, which also sets the cloud flag.
        // Core doesn't report when its flag sync is done, so the restore waits for the next change to IAP.
        void OnProjectStateChanged()
        {
            if (!CloudProjectSettings.projectBound)
            {
                m_RestoreServiceAfterLink = false;
            }
            else if (m_ServiceOnWhileUnlinked)
            {
                m_ServiceOnWhileUnlinked = false;
                m_RestoreServiceAfterLink = true;
            }
        }

        void EnableOperationCompleted()
        {
            m_RestoreServiceAfterLink = false;
            m_StateMachine.ProcessEvent(true);

            RefreshDetailUI();
        }

        void DisableOperationCompleted()
        {
            // Unlinking also turns IAP off, but sends no flag request to wait for, so only a linked project restores.
            if (m_RestoreServiceAfterLink && CloudProjectSettings.projectBound)
            {
                m_RestoreServiceAfterLink = false;
                // Core's Disable also sends the cloud flag. Enabling once that request is done keeps the two
                // requests in order, so the cloud project ends up with IAP on.
                var enabler = (EditorGameServiceFlagEnabler)m_Service.Enabler;
                void EnableAfterFlagRequest()
                {
                    enabler.ServiceFlagRequestComplete -= EnableAfterFlagRequest;
                    enabler.Enable();
                }

                enabler.ServiceFlagRequestComplete += EnableAfterFlagRequest;
            }

            m_StateMachine.ProcessEvent(false);

            RefreshDetailUI();
        }
    }
}
#endif

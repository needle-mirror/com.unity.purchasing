using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Purchasing.Editor.Authoring.UI;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Editor.Authoring.Import.UI
{
    /// <summary>
    /// Manages the left panel of the Catalog Import window: fetch config,
    /// step headers, status messages, and the provider tab toolbar.
    /// </summary>
    class CatalogImportProviderPanelView
    {
        const string k_HiddenClass = "hidden";
        const long k_ReclassifyDebounceMs = 300;

        readonly VisualElement m_Root;
        readonly CatalogImportState m_State;
        readonly CatalogImportController m_Controller;

        readonly List<Button> m_TabButtons = new();

        HelpBox m_ProjectNotLinkedBox;
        VisualElement m_FetcherConfigContainer;
        Button m_FetchButton;
        HelpBox m_FetchStatusHelpBox;
        BrowsablePathField m_OutputFolderField;
        Button m_ConfirmButton;
        Button m_ExportCsvButton;
        HelpBox m_StatusHelpBox;

        CatalogImportProvider m_ActiveProvider;
        Action<string> m_OutputFolderChangedCallback;
        IVisualElementScheduledItem m_ReclassifySchedule;

        public CatalogImportProviderPanelView(VisualElement root, CatalogImportState state, CatalogImportController controller)
        {
            m_Root = root ?? throw new ArgumentNullException(nameof(root));
            m_State = state ?? throw new ArgumentNullException(nameof(state));
            m_Controller = controller ?? throw new ArgumentNullException(nameof(controller));
        }

        /// <summary>
        /// Queries elements, builds the tab toolbar, wires callbacks, and subscribes to state events.
        /// Call this once after construction.
        /// </summary>
        public void Initialize()
        {
            QueryElements();
            BuildTabToolbar();
            WireCallbacks();

            LoadProviderFields(m_Controller.ActiveProvider);

            m_State.StateChanged += Refresh;
            m_Controller.TabSwitched += OnTabSwitched;
            CloudProjectSettingsEventManager.instance.projectStateChanged += RefreshProjectLinkedState;

            Refresh();
        }

        void QueryElements()
        {
            m_ProjectNotLinkedBox = m_Root.Q<HelpBox>("ProjectNotLinkedBox");
            SetupProjectNotLinkedBox();

            m_FetcherConfigContainer = m_Root.Q<VisualElement>("FetcherConfigContainer");
            m_FetchButton = m_Root.Q<Button>("FetchButton");
            m_FetchStatusHelpBox = m_Root.Q<HelpBox>("FetchStatusHelpBox");

            var step3 = m_Root.Q<VisualElement>("Step3Container");

            var step3Header = new Label("Step 2: Import or Export");
            step3Header.AddToClassList("section-header");
            step3.Add(step3Header);

            m_OutputFolderField = new BrowsablePathField("Import New");
            m_OutputFolderField.LabelMinWidth = 150;
            var outputFolderRow = new VisualElement();
            outputFolderRow.AddToClassList("output-folder-row");
            outputFolderRow.Add(m_OutputFolderField);
            step3.Add(outputFolderRow);

            m_ConfirmButton = new Button { name = "ConfirmButton", text = "Apply" };
            step3.Add(m_ConfirmButton);

            step3.Add(new HelpBox(
                "Any modified SKU will only update the existing .ucat file, " +
                "whereas any new SKU will create new .ucat files at the import destination.",
                HelpBoxMessageType.Info));

            var orLabel = new Label("OR");
            orLabel.AddToClassList("or-separator");
            step3.Add(orLabel);

            m_ExportCsvButton = new Button { name = "ExportCsvButton", text = "Export" };
            step3.Add(m_ExportCsvButton);

            step3.Add(new HelpBox(
                "All SKU files will be exported into one .catalog.csv file.",
                HelpBoxMessageType.Info));

            m_StatusHelpBox = m_Root.Q<HelpBox>("StatusHelpBox");
        }

        void BuildTabToolbar()
        {
            var tabBar = m_Root.Q<VisualElement>("TabBar");
            m_TabButtons.Clear();

            var providers = m_Controller.Providers;

            for (var i = 0; i < providers.Count; i++)
            {
                var index = i;
                var provider = providers[i];
                var btn = new Button(() => m_Controller.TrySwitchTab(index));
                btn.AddToClassList("provider-tab");

                var icon = new VisualElement();
                icon.AddToClassList("tab-icon");

                if (!string.IsNullOrEmpty(provider.IconCssClass))
                {
                    icon.AddToClassList(provider.IconCssClass);
                }

                btn.Add(icon);
                btn.Add(new Label(provider.Name));
                tabBar.Add(btn);
                m_TabButtons.Add(btn);
            }

            RefreshTabHighlight();
        }

        void WireCallbacks()
        {
            m_FetchButton.clicked += m_Controller.OnFetchClicked;
            m_ConfirmButton.clicked += m_Controller.OnConfirmClicked;
            m_ExportCsvButton.clicked += m_Controller.OnExportCsvClicked;

            m_OutputFolderChangedCallback = value =>
            {
                m_State.OutputFolder = value;
                if (m_Controller.ActiveProvider != null)
                {
                    m_Controller.ActiveProvider.OutputFolder = value;
                }

                if (m_ReclassifySchedule == null)
                {
                    m_ReclassifySchedule = m_Root.schedule.Execute(m_Controller.OnOutputFolderChanged);
                }
                m_ReclassifySchedule.ExecuteLater(k_ReclassifyDebounceMs);
            };
            m_OutputFolderField.ValueChanged += m_OutputFolderChangedCallback;
        }

        internal void Dispose()
        {
            if (m_State != null)
            {
                m_State.StateChanged -= Refresh;
            }

            if (m_Controller != null)
            {
                m_Controller.TabSwitched -= OnTabSwitched;
            }

            CloudProjectSettingsEventManager.instance.projectStateChanged -= RefreshProjectLinkedState;

            if (m_FetchButton != null)
            {
                m_FetchButton.clicked -= m_Controller.OnFetchClicked;
            }

            if (m_ConfirmButton != null)
            {
                m_ConfirmButton.clicked -= m_Controller.OnConfirmClicked;
            }

            if (m_ExportCsvButton != null)
            {
                m_ExportCsvButton.clicked -= m_Controller.OnExportCsvClicked;
            }

            if (m_OutputFolderField != null)
            {
                m_OutputFolderField.ValueChanged -= m_OutputFolderChangedCallback;
            }
        }

        void SetupProjectNotLinkedBox()
        {
            m_ProjectNotLinkedBox.text = "No connection to Unity Services. Link your project in <color=#4C7EFF>Project Settings</color>.";

            var label = m_ProjectNotLinkedBox.Q<Label>();
            if (label != null)
            {
                label.enableRichText = true;
            }

            m_ProjectNotLinkedBox.RegisterCallback<ClickEvent>(_ => SettingsService.OpenProjectSettings("Project/Services"));
        }

        void RefreshProjectLinkedState()
        {
            var isLinked = !string.IsNullOrEmpty(CloudProjectSettings.projectId);
            m_ProjectNotLinkedBox.EnableInClassList(k_HiddenClass, isLinked);
            m_FetchButton.SetEnabled(isLinked && !m_State.IsFetching);
        }

        void Refresh()
        {
            RefreshProjectLinkedState();
            RefreshFetchStatus();
            RefreshConfirmButtons();
            RefreshStatusMessage();
        }

        void RefreshFetchStatus()
        {
            if (string.IsNullOrEmpty(m_State.FetchStatusMessage))
            {
                m_FetchStatusHelpBox.AddToClassList(k_HiddenClass);
            }
            else
            {
                m_FetchStatusHelpBox.text = m_State.FetchStatusMessage;
                m_FetchStatusHelpBox.messageType = HelpBoxMessageType.Info;
                m_FetchStatusHelpBox.RemoveFromClassList(k_HiddenClass);
            }
        }

        void RefreshConfirmButtons()
        {
            var canConfirm = m_State.DataFetched && m_State.FetchedEntries.Count > 0;
            m_ConfirmButton.SetEnabled(canConfirm);
            m_ExportCsvButton.SetEnabled(canConfirm);
        }

        void RefreshStatusMessage()
        {
            if (string.IsNullOrEmpty(m_State.StatusMessage))
            {
                m_StatusHelpBox.AddToClassList(k_HiddenClass);
            }
            else
            {
                m_StatusHelpBox.text = m_State.StatusMessage;
                m_StatusHelpBox.messageType = HelpBoxMessageType.Info;
                m_StatusHelpBox.RemoveFromClassList(k_HiddenClass);
            }
        }

        void RefreshTabHighlight()
        {
            for (var i = 0; i < m_TabButtons.Count; i++)
            {
                if (i == m_Controller.SelectedTab)
                {
                    m_TabButtons[i].AddToClassList("tab--active");
                }
                else
                {
                    m_TabButtons[i].RemoveFromClassList("tab--active");
                }
            }
        }

        void OnTabSwitched(int tabIndex)
        {
            RefreshTabHighlight();
            LoadProviderFields(m_Controller.ActiveProvider);
            RefreshProviderConfigUI(m_Controller.ActiveProvider);
        }

        void LoadProviderFields(CatalogImportProvider provider)
        {
            if (provider == null)
            {
                return;
            }

            m_OutputFolderField.Value = provider.OutputFolder;
            m_State.OutputFolder = provider.OutputFolder;
        }

        /// <summary>
        /// Rebuilds the fetcher config UI for the given provider.
        /// Call whenever the active provider changes.
        /// </summary>
        internal void RefreshProviderConfigUI(CatalogImportProvider provider)
        {
            m_ActiveProvider = provider;

            m_FetcherConfigContainer.Clear();

            if (provider?.FetcherConfigDrawer != null)
            {
                var fetcherConfigUI = provider.FetcherConfigDrawer.CreateConfigUI();
                if (fetcherConfigUI != null)
                {
                    m_FetcherConfigContainer.Add(fetcherConfigUI);
                }
            }
        }

    }
}

using System.Linq;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Authoring;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;
using UnityEditor.Purchasing.Editor.Authoring.Model;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Editor.Authoring.UI
{
    [CustomEditor(typeof(CatalogCsvAsset))]
    class CatalogCsvInspector : UnityEditor.Editor
    {
        const string k_StylePath =
            "Packages/com.unity.purchasing/Editor/Authoring/UI/Assets/CatalogCsvInspectorStyle.uss";

        string m_AssetPath;
        bool m_PendingShowCsv;
        Button m_ApplyButton;
        Button m_RevertButton;
        RadioButton m_CsvRadio;
        RadioButton m_UcatRadio;
        CatalogCsvInspectorConfig m_ProductsConfig;
        SerializedObject m_SerializedProductsConfig;

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            root.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(k_StylePath));

            m_AssetPath = AssetDatabase.GetAssetPath(target);

            var csvAssets = PurchasingAuthoringServiceProvider.GetService<ObservableCatalogCsvAssets>();
            var deploymentItem = csvAssets.GetDeploymentItem(m_AssetPath);

            AddToggleSection(root);
            AddApplyRevertButtons(root);
            AddSeparator(root);
            AddProductsField(root, deploymentItem);
            AddStatesSection(root, deploymentItem);
            CatalogInspectorHelper.AddDeploymentFooter(root, m_AssetPath, deploymentItem);

            return root;
        }

        void AddProductsField(VisualElement root, CatalogCsvDeploymentItem deploymentItem)
        {
            m_ProductsConfig = CreateInstance<CatalogCsvInspectorConfig>();
            m_ProductsConfig.Initialize(deploymentItem?.CatalogItems);
            m_SerializedProductsConfig = new SerializedObject(m_ProductsConfig);
            m_SerializedProductsConfig.Update();

            var inspector = new InspectorElement();
            inspector.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                var scriptField = inspector.Q("PropertyField:m_Script");
                scriptField?.RemoveFromHierarchy();
            });
            root.Add(inspector);
            inspector.Bind(m_SerializedProductsConfig);
        }

        static void AddSeparator(VisualElement root)
        {
            var separator = new VisualElement();
            separator.AddToClassList("separator");
            root.Add(separator);
        }

        void AddToggleSection(VisualElement root)
        {
            var foldout = new Foldout { text = "Deployment Window", value = true };
            foldout.AddToClassList("products-foldout");

            m_PendingShowCsv = CatalogUserSettings.IsShownAsCsv(m_AssetPath);

            var row = new VisualElement();
            row.AddToClassList("toggle-row");

            var viewAsLabel = new Label("Show as:");
            viewAsLabel.AddToClassList("toggle-label");

            var csvLabel = new Label(".catalog.csv");
            csvLabel.AddToClassList("csv-label");

            m_CsvRadio = new RadioButton { value = m_PendingShowCsv };
            m_CsvRadio.AddToClassList("radio-button");

            m_UcatRadio = new RadioButton { value = !m_PendingShowCsv };
            m_UcatRadio.AddToClassList("ucat-radio");

            var ucatLabel = new Label("individual .ucat");

            m_CsvRadio.RegisterValueChangedCallback(evt =>
            {
                m_PendingShowCsv = evt.newValue;
                m_UcatRadio.SetValueWithoutNotify(!evt.newValue);
                UpdateHasChanges();
            });

            m_UcatRadio.RegisterValueChangedCallback(evt =>
            {
                m_PendingShowCsv = !evt.newValue;
                m_CsvRadio.SetValueWithoutNotify(!evt.newValue);
                UpdateHasChanges();
            });

            row.Add(viewAsLabel);
            row.Add(csvLabel);
            row.Add(m_CsvRadio);
            row.Add(m_UcatRadio);
            row.Add(ucatLabel);
            foldout.Add(row);
            root.Add(foldout);
        }

        void AddApplyRevertButtons(VisualElement root)
        {
            var buttonRow = new VisualElement();
            buttonRow.AddToClassList("button-row");

            m_ApplyButton = new Button(OnApplyClicked) { text = "Apply" };
            m_ApplyButton.AddToClassList("grow-button");
            m_ApplyButton.SetEnabled(false);

            m_RevertButton = new Button(OnRevertClicked) { text = "Revert" };
            m_RevertButton.AddToClassList("grow-button");
            m_RevertButton.SetEnabled(false);

            buttonRow.Add(m_ApplyButton);
            buttonRow.Add(m_RevertButton);
            root.Add(buttonRow);
        }

        void UpdateHasChanges()
        {
            var hasChanges = m_PendingShowCsv != CatalogUserSettings.IsShownAsCsv(m_AssetPath);
            m_ApplyButton?.SetEnabled(hasChanges);
            m_RevertButton?.SetEnabled(hasChanges);
        }

        void OnApplyClicked()
        {
            CatalogUserSettings.SetShownAsCsv(m_AssetPath, m_PendingShowCsv);
            m_ApplyButton?.SetEnabled(false);
            m_RevertButton?.SetEnabled(false);
        }

        static void AddStatesSection(VisualElement root, CatalogCsvDeploymentItem deploymentItem)
        {
            if (deploymentItem == null || !deploymentItem.States.Any())
            {
                return;
            }

            foreach (var state in deploymentItem.States)
            {
                var messageType = state.Level switch
                {
                    SeverityLevel.Error => HelpBoxMessageType.Error,
                    SeverityLevel.Warning => HelpBoxMessageType.Warning,
                    _ => HelpBoxMessageType.Info
                };
                var text = state.Description;
                if (!string.IsNullOrEmpty(state.Detail))
                {
                    text += "\n" + state.Detail;
                }
                root.Add(new HelpBox { messageType = messageType, text = text });
            }
        }

        void OnRevertClicked()
        {
            m_PendingShowCsv = CatalogUserSettings.IsShownAsCsv(m_AssetPath);
            m_CsvRadio?.SetValueWithoutNotify(m_PendingShowCsv);
            m_UcatRadio?.SetValueWithoutNotify(!m_PendingShowCsv);
            m_ApplyButton?.SetEnabled(false);
            m_RevertButton?.SetEnabled(false);
        }
    }
}

using System;
using System.Linq;
using System.Text;
using UnityEditor.Purchasing.Editor.Authoring.Core.Validations;
using UnityEditor.Purchasing.Editor.Authoring.Model;
using UnityEditor.Purchasing.Editor.Authoring.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Authoring
{
    [CustomEditor(typeof(CatalogItemAsset))]
    class CatalogItemAssetInspector : UnityEditor.Editor
    {
        const string k_Uxml = "Packages/com.unity.purchasing/Editor/Authoring/UI/Assets/CatalogAssetInspector.uxml";
        const string k_MinimumPriceTooltip = "Prices below the minimum might not be supported by certain payment processors. Verify the minimum supported value with the processor you plan to use.";

        VisualElement m_RootElement;
        VisualElement m_InfoRootElement;
        Button m_ApplyButton;
        Button m_RevertButton;

        CatalogItemAsset m_TargetItemAsset;
        CatalogItemInspectorConfig m_ItemInspectorConfig;
        InspectorElement m_CatalogItemAssetInspector;
        SerializedObject m_SerializedObjectOriginal;
        SerializedObject m_SerializedObjectCurrent;

        StringBuilder m_Warnings = new StringBuilder();

        public override VisualElement CreateInspectorGUI()
        {
            m_TargetItemAsset = target as CatalogItemAsset;

            m_RootElement = new VisualElement();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_Uxml);
            visualTree.CloneTree(m_RootElement);

#if !UNITY_2022_1_OR_NEWER
            //2021 does not support nested inspector elements properly
            var applyRevert = m_RootElement.Q<VisualElement>("ContainerApplyRevert");
            m_RootElement.Remove(applyRevert);
            var txtEle = new TextField() { multiline = true, isReadOnly = true };
            m_RootElement.Insert(0, txtEle);
            txtEle.value = ReadResourceBody(targets[0]);
#else
            CreateInspector();
            BuildCatalogItemInspectorConfig();
            InitializeInfoRootElement();
            InitializeSerializedObjects();
            UpdatePricingDetailsWarnings();
            InitializeApplyRevertButtons();
            CatalogInspectorHelper.AddDeploymentFooter(
                m_RootElement,
                AssetDatabase.GetAssetPath(target),
                m_TargetItemAsset.CatalogEntryDeploymentItem);
#endif


            return m_RootElement;
        }

#if !UNITY_2022_1_OR_NEWER
        static string ReadResourceBody(UnityEngine.Object resource)
        {
            var path = AssetDatabase.GetAssetPath(resource);
            var lines = File.ReadLines(path).Take(75).ToList();
            if (lines.Count == 75)
            {
                lines.Add("...");
            }
            return string.Join(Environment.NewLine, lines);
        }
#endif

        void CreateInspector()
        {
            var container = m_RootElement.Q("ContainerConfig");
            m_CatalogItemAssetInspector = new InspectorElement();
            container.Add(m_CatalogItemAssetInspector);
        }

        void BuildCatalogItemInspectorConfig()
        {
            var configTarget = target as CatalogItemAsset;
            var catalogListingId = configTarget is null
                ? string.Empty
                : System.IO.Path.GetFileNameWithoutExtension(configTarget.Path);
            m_ItemInspectorConfig = CreateInstance<CatalogItemInspectorConfig>();
            m_ItemInspectorConfig.Initialize(configTarget?.CatalogEntryDeploymentItem?.CatalogItem);
        }

        void InitializeSerializedObjects()
        {
            m_SerializedObjectOriginal = new SerializedObject(m_ItemInspectorConfig);
            m_SerializedObjectCurrent = new SerializedObject(m_ItemInspectorConfig);

            m_CatalogItemAssetInspector.Unbind();
            m_CatalogItemAssetInspector.Bind(m_SerializedObjectCurrent);
            m_CatalogItemAssetInspector.TrackSerializedObjectValue(m_SerializedObjectCurrent, SerializedObjectValueChanged);
            BindWebshopVisibility(m_CatalogItemAssetInspector, m_SerializedObjectCurrent);
        }

        static readonly string[] s_WebshopFieldPaths =
        {
            nameof(CatalogItemInspectorConfig.Categories),
            nameof(CatalogItemInspectorConfig.HdImages),
            nameof(CatalogItemInspectorConfig.Promotion),
        };

        static void BindWebshopVisibility(VisualElement root, SerializedObject so)
        {
            var toggleProp = so.FindProperty(nameof(CatalogItemInspectorConfig.IsWebshopAvailable));
            if (toggleProp is null)
                return;

            void Apply()
            {
                var display = toggleProp.boolValue ? DisplayStyle.Flex : DisplayStyle.None;
                foreach (var field in root.Query<PropertyField>().ToList())
                {
                    if (System.Array.IndexOf(s_WebshopFieldPaths, field.bindingPath) >= 0)
                        field.style.display = display;
                }
            }

            root.TrackPropertyValue(toggleProp, _ => Apply());
            // InspectorElement materializes its PropertyField children asynchronously, so the
            // first Apply must wait a frame to see them. The toggle callback handles updates after.
            root.schedule.Execute(Apply).StartingIn(0);
        }

        void SerializedObjectValueChanged(SerializedObject obj)
        {
            ClearInfoElements();
            var areObjectsEqual = AreSerializedObjectsEqual(m_SerializedObjectOriginal, m_SerializedObjectCurrent);
            UpdateApplyRevertButtons(!areObjectsEqual);
            UpdatePricingDetailsWarnings();
#if UNITY_2022_1_OR_NEWER
            hasUnsavedChanges = !areObjectsEqual;
#endif
        }

        void InitializeInfoRootElement()
        {
            m_InfoRootElement = m_RootElement.Q<Label>("InfoLabel");
        }

        void ClearInfoElements()
        {
            m_InfoRootElement.Clear();
        }

        void UpdatePricingDetailsWarnings()
        {
            var item = m_TargetItemAsset.CatalogEntryDeploymentItem.CatalogItem;
            item.PricingDetails?.ForEach(d =>
            {
                if (!MinimumPriceValidation.IsPriceValid(d.CurrencyCode, d.Amount, out double minimumPrice))
                {
                    HelpBox helpBox = new HelpBox();
                    helpBox.messageType = HelpBoxMessageType.Warning;
                    helpBox.text = $"{d.CurrencyCode} is below minimum price {minimumPrice}";
                    helpBox.tooltip = k_MinimumPriceTooltip;

                    m_InfoRootElement.Add(helpBox);
                }
            });

            if (!item.IsWebshopAvailable && item.PricingDetails != null
                && item.PricingDetails.Any(p => p.IsWebshopPriceSet))
            {
                m_InfoRootElement.Add(new HelpBox(
                    "A Webshop price is set, but \"Webshop availability\" is off. The price will not be used by the Webshop until you toggle availability on.",
                    HelpBoxMessageType.Warning));
            }
        }

        void InitializeApplyRevertButtons()
        {
            m_ApplyButton = m_RootElement.Q<Button>("ApplyButton");
            m_ApplyButton.clicked += SaveChanges;
            m_RevertButton = m_RootElement.Q<Button>("RevertButton");
            m_RevertButton.clicked += DiscardChanges;

            UpdateApplyRevertButtons(false);
        }

        void UpdateApplyRevertButtons(bool toggle)
        {
            m_RevertButton.SetEnabled(toggle);
            m_ApplyButton.SetEnabled(toggle);
        }

        void SaveAssetChanges()
        {
            m_TargetItemAsset.CopyFrom((CatalogItemInspectorConfig)m_SerializedObjectCurrent.targetObject);
            m_TargetItemAsset.SaveToDisk();

            InitializeSerializedObjects();
        }

#if UNITY_2022_1_OR_NEWER
        public override void SaveChanges()
        {
            SaveAssetChanges();
            base.SaveChanges();
            UpdateApplyRevertButtons(false);
            AssetDatabase.Refresh();
        }

        public override void DiscardChanges()
        {
            RevertAssetChanges();
            base.DiscardChanges();
            UpdateApplyRevertButtons(false);
        }
#else
        public void SaveChanges()
        {
            SaveAssetChanges();
            UpdateApplyRevertButtons(false);
        }

        public void DiscardChanges()
        {
            RevertAssetChanges();
            UpdateApplyRevertButtons(false);
        }
#endif

        void RevertAssetChanges()
        {
            BuildCatalogItemInspectorConfig();
            InitializeSerializedObjects();
            UpdateApplyRevertButtons(false);
        }

        static bool AreSerializedObjectsEqual(SerializedObject obj1, SerializedObject obj2)
        {
            if (obj1 == null || obj2 == null)
                return false;

            var iterator1 = obj1.GetIterator();
            var iterator2 = obj2.GetIterator();

            while (iterator1.NextVisible(true) && iterator2.NextVisible(true))
            {
                if (iterator1.propertyType != iterator2.propertyType || iterator1.name != iterator2.name)
                    return false;

                if (!SerializedProperty.DataEquals(iterator1, iterator2))
                    return false;
            }

            return true;
        }
    }
}

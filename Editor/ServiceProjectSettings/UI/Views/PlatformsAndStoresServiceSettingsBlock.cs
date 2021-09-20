using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    abstract class PlatformsAndStoresServiceSettingsBlock : IPurchasingSettingsUIBlock
    {
        const string k_TagClass = "platform-tag";
        const string k_TagContainerClass = "tag-container";

        const string k_CurrentBuildTargetSectionName = "CurrentBuildTargetSection";
        const string k_CurrentStoreSectionName = "CurrentStoreSection";
        const string k_SupportedStoresSectionName = "SupportedStoresSection";
        const string k_OtherStoresSectionName = "OtherStoresSection";
        const string k_Label = "Label";

        protected VisualElement currentStoreSection { get; private set; }

        public static PlatformsAndStoresServiceSettingsBlock CreateStateSpecificBlock(bool enabled)
        {
            if (enabled)
            {
                return new PlatformsAndStoresEnabledServiceSettingsBlock();
            }
            else
            {
                return new PlatformsAndStoresDisabledServiceSettingsBlock();
            }
        }

        public VisualElement GetUIBlockElement()
        {
            return SetupPlatformAndStoresBlock();
        }

        VisualElement SetupPlatformAndStoresBlock()
        {
            var rootElement = SettingsUIUtils.CloneUIFromTemplate(UIResourceUtils.platformSupportUxmlPath);

            currentStoreSection = rootElement.Q(k_CurrentStoreSectionName);

            SetupStyleSheets(rootElement);
            PopulateSections(rootElement);

            return rootElement;
        }

        static void SetupStyleSheets(VisualElement rootElement)
        {
            rootElement.AddStyleSheetPath(UIResourceUtils.platformSupportCommonUssPath);
            rootElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? UIResourceUtils.platformSupportDarkUssPath : UIResourceUtils.platformSupportLightUssPath);
        }

        void PopulateSections(VisualElement rootElement)
        {
            var currentBuildTargetSection = rootElement.Q(k_CurrentBuildTargetSectionName);
            var otherStoresSection = rootElement.Q(k_OtherStoresSectionName);

            PopulateStateSensitiveSections(rootElement, currentBuildTargetSection, otherStoresSection);
            PopulateSupportedStoresSection(rootElement.Q(k_SupportedStoresSectionName));
        }

        protected abstract void PopulateStateSensitiveSections(VisualElement rootElement, VisualElement currentBuildTargetSection, VisualElement otherStoresSection);
        protected abstract void PopulateSupportedStoresSection(VisualElement baseElement);

        protected static void PopulateStores(VisualElement baseElement, IEnumerable<string> stores)
        {
            var tagContainer = GetClearedTagContainer(baseElement);

            foreach (var store in stores)
            {
                tagContainer.Add(MakePlatformStoreTag(store));
            }
        }

        protected static VisualElement GetClearedTagContainer(VisualElement baseElement)
        {
            var tagContainer = GetTagContainer(baseElement);
            tagContainer.Clear();
            return tagContainer;
        }

        protected static VisualElement GetTagContainer(VisualElement baseElement)
        {
            return baseElement.Q(className: k_TagContainerClass);
        }

        protected static VisualElement MakePlatformStoreTag(string assetDisplayName)
        {
            var storeLabel = MakeLabel();

            var label = storeLabel.Q(name: k_Label) as Label;
            AddText(label, assetDisplayName);

            return storeLabel;
        }

        static VisualElement MakeLabel()
        {
            var label = SettingsUIUtils.CloneUIFromTemplate(UIResourceUtils.labelUxmlPath);
            label.AddToClassList(k_TagClass);
            return label;
        }
        static void AddText(Label label, string assetDisplayName)
        {
            if (label == null)
            {
                return;
            }

            label.text = assetDisplayName;
        }
    }
}

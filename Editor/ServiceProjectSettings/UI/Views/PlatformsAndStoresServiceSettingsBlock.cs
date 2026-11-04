using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    abstract class PlatformsAndStoresServiceSettingsBlock : IPurchasingSettingsUIBlock
    {
        static readonly List<string> k_StoreNames = new List<string>()
        {
            GooglePlay.DisplayName,
            AppleAppStore.DisplayName,
            MacAppStore.DisplayName,
            XboxStore.DisplayName
        };

        const string k_ValueClass = "editor-setup-value";
        const string k_JumpToBuildSettingsButtonName = "JumpToBuildSettingsButton";

        const string k_CurrentBuildTargetSectionName = "CurrentBuildTargetSection";
        const string k_CurrentStoreSectionName = "CurrentStoreSection";
        const string k_SupportedStoresSectionName = "SupportedStoresSection";
        const string k_OtherStoresSectionName = "OtherStoresSection";

        protected VisualElement currentStoreSection { get; set; }

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
            SetupJumpToBuildSettingsButton(rootElement.Q<Button>(k_JumpToBuildSettingsButtonName));
            PopulateSections(rootElement);
            FinalizeSetupPlatformAndStoresBlock();

            return rootElement;
        }

        static void SetupStyleSheets(VisualElement rootElement)
        {
            rootElement.AddStyleSheetPath(UIResourceUtils.platformSupportCommonUssPath);
            rootElement.AddStyleSheetPath(EditorGUIUtility.isProSkin ? UIResourceUtils.platformSupportDarkUssPath : UIResourceUtils.platformSupportLightUssPath);
        }

        // Unity 6 replaced the Build Settings window with Build Profiles, and opens it from the same entry point.
        static void SetupJumpToBuildSettingsButton(Button button)
        {
            if (button == null)
            {
                return;
            }

#if !UNITY_6000_0_OR_NEWER
            button.text = "Jump to Build Settings";
#endif
            button.clicked += BuildPlayerWindow.ShowBuildPlayerWindow;
        }

        void PopulateSections(VisualElement rootElement)
        {
            var currentBuildTargetSection = rootElement.Q(k_CurrentBuildTargetSectionName);
            var otherStoresSection = rootElement.Q(k_OtherStoresSectionName);

            PopulateStateSensitiveSections(rootElement, currentBuildTargetSection, otherStoresSection);
            PopulateStores(rootElement.Q(k_SupportedStoresSectionName), GetStoresForState());
        }

        protected abstract void PopulateStateSensitiveSections(VisualElement rootElement, VisualElement currentBuildTargetSection, VisualElement otherStoresSection);

        protected static void PopulateStores(VisualElement baseElement, IEnumerable<string> stores)
        {
            SetValue(baseElement, string.Join(", ", stores));
        }

        protected static void SetValue(VisualElement baseElement, string value)
        {
            var label = baseElement?.Q<Label>(className: k_ValueClass);
            if (label != null)
            {
                label.text = value;
            }
        }

        protected abstract void FinalizeSetupPlatformAndStoresBlock();

        protected abstract IEnumerable<string> GetStoresForState();

        protected static IEnumerable<string> GetAllStores()
        {
            return k_StoreNames;
        }
    }
}

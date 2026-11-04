using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.UIElements;
using UnityEngine.Purchasing;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    class PlatformsAndStoresEnabledServiceSettingsBlock : PlatformsAndStoresServiceSettingsBlock
    {
        PopupField<string> currentStoreField { get; set; }

        protected override void FinalizeSetupPlatformAndStoresBlock()
        {
            RegisterOnTargetChange();
        }

        void RegisterOnTargetChange()
        {
            if (EditorUserBuildSettings.activeBuildTargetGroup == BuildTargetGroup.Android)
            {
                RegisterOnAndroidTargetChange();
            }
        }

        void RegisterOnAndroidTargetChange()
        {
            UnregisterOnAndroidTargetChange();
            UnityPurchasingEditor.OnAndroidTargetChange += OnAndroidTargetChange;
        }

        void UnregisterOnAndroidTargetChange()
        {
            UnityPurchasingEditor.OnAndroidTargetChange -= OnAndroidTargetChange;
        }

        void OnAndroidTargetChange(AppStore appStore)
        {
            if (!appStore.IsAndroid())
            {
                return;
            }

            var stores = BuildTargetGroup.Android.ToAppStores();
            if (currentStoreField != null && stores.Contains(appStore))
            {
                currentStoreField.SetValueWithoutNotify(appStore.ToDisplayName());
            }
        }

        protected override void PopulateStateSensitiveSections(VisualElement rootElement, VisualElement currentBuildTargetSection, VisualElement otherStoresSection)
        {
            PopulateCurrentBuildTarget(currentBuildTargetSection);
            PopulateCurrentStoreTarget(currentStoreSection);
            PopulateOtherStores(otherStoresSection);
        }

        static void PopulateCurrentBuildTarget(VisualElement baseElement)
        {
            PopulatePlatform(baseElement, GetCurrentBuildTarget());
        }

        void PopulateCurrentStoreTarget(VisualElement baseElement)
        {
            var stores = EditorUserBuildSettings.activeBuildTargetGroup.ToAppStoreDisplayNames().ToList();
            if (baseElement == null || stores.Count == 0)
            {
                baseElement?.RemoveFromHierarchy();
                return;
            }

            currentStoreField = new PopupField<string>(stores, Math.Max(0, stores.IndexOf(GetCurrentStoreTarget())));
            currentStoreField.AddToClassList("editor-setup-dropdown");
            currentStoreField.RegisterValueChangedCallback(evt => OnCurrentStoreTargetChanged(evt.newValue));
            baseElement.Add(currentStoreField);
        }

        void OnCurrentStoreTargetChanged(string storeName)
        {
            var store = storeName.ToAppStoreFromDisplayName();

            if (store.IsAndroid())
            {
                OnCurrentStoreTargetChanged(store);
            }
        }

        void OnCurrentStoreTargetChanged(AppStore store)
        {
            var actualStore = UnityPurchasingEditor.TryTargetAndroidStore(store);

            if (actualStore != store)
            {
                OnAndroidTargetChange(actualStore);
            }
        }

        static void PopulateOtherStores(VisualElement baseElement)
        {
            PopulateStores(baseElement, GetOtherStores());
        }

        protected override IEnumerable<string> GetStoresForState()
        {
            return GetSupportedStores();
        }

        static IEnumerable<string> GetSupportedStores()
        {
            return GetSupportedStoresIncludingTarget();
        }

        static IEnumerable<string> GetSupportedStoresIncludingTarget()
        {
            return new List<string>(EditorUserBuildSettings.activeBuildTargetGroup.ToAppStoreDisplayNames());
        }

        static IEnumerable<string> GetOtherStores()
        {
            var supportedStores = GetSupportedStoresIncludingTarget();
            var otherStores = GetAllStores().ToList();
            otherStores.RemoveAll(store => supportedStores.Contains(store));

            return otherStores;
        }

        static string GetCurrentBuildTarget()
        {
            return EditorUserBuildSettings.activeBuildTargetGroup.ToPlatformDisplayName();
        }

        static string GetCurrentStoreTarget()
        {
            var currentStoreTargets = EditorUserBuildSettings.activeBuildTargetGroup.ToAppStoreDisplayNames();

            if (currentStoreTargets.Count == 1)
            {
                return currentStoreTargets.First();
            }

            return UnityPurchasingEditor.ConfiguredAppStore().ToDisplayName();
        }

        static void PopulatePlatform(VisualElement baseElement, string platform)
        {
            SetValue(baseElement, platform);
        }
    }
}

using System;
using UnityEditor.Build;
using UnityEditor.Purchasing.UI.Presenters;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    class PlatformsAndStoresEnabledServiceSettingsBlock : PlatformsAndStoresServiceSettingsBlock
    {
        IMGUIContainerPopupAdapter currentStoreTargetContainer { get; set; }
        PlatformsStoreSettingsPresenter m_Presenter;

        internal PlatformsAndStoresEnabledServiceSettingsBlock()
        {
            RegisterOnTargetChange();
            m_Presenter = new PlatformsStoreSettingsPresenter();
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
            UpdateCurrentStoreTarget(appStore);
        }

        void UpdateCurrentStoreTarget(AppStore appStore)
        {
            if (GetTagContainer(currentStoreSection) is IMGUIContainer field &&
                currentStoreTargetContainer != null && currentStoreTargetContainer.container == field)
            {
                currentStoreTargetContainer.index = m_Presenter.GetIndexOfAndroidStore(appStore);
            }
        }

        protected override void PopulateStateSensitiveSections(VisualElement rootElement, VisualElement currentBuildTargetSection, VisualElement otherStoresSection)
        {
            PopulateCurrentBuildTarget(currentBuildTargetSection);
            PopulateCurrentStoreTarget(currentStoreSection);
            PopulateOtherStores(otherStoresSection);
        }

        protected override void PopulateSupportedStoresSection(VisualElement baseElement)
        {
            PopulateStores(baseElement, m_Presenter.GetSupportedStores());
        }

        static void PopulateCurrentBuildTarget(VisualElement baseElement)
        {
            PopulatePlatform(baseElement, GetCurrentBuildTarget());
        }

        void PopulateCurrentStoreTarget(VisualElement baseElement)
        {
            if (!(GetTagContainer(baseElement) is IMGUIContainer field))
            {
                return;
            }

            currentStoreTargetContainer = new IMGUIContainerPopupAdapter
            {
                popupValueChangedAction = OnCurrentStoreTargetChanged,
                options = m_Presenter.GetCurrentStoreTargetContainerOptions(),
                index = m_Presenter.GetCurrentStoreTargetContainerIndex(),
                container = field
            };
        }

        void OnCurrentStoreTargetChanged(string e)
        {
            var store = e.ToAppStoreFromDisplayName();

            if (store == AppStore.NotSpecified)
            {
                OnCurrentStoreTargetChanged(store);
            }
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

        void PopulateOtherStores(VisualElement baseElement)
        {
            PopulateStores(baseElement, m_Presenter.GetOtherStores());
        }

        static string GetCurrentBuildTarget()
        {
            return EditorUserBuildSettings.activeBuildTargetGroup.ToPlatformDisplayName();
        }

        static void PopulatePlatform(VisualElement baseElement, string platform)
        {
            var tagContainer = GetClearedTagContainer(baseElement);

            tagContainer.Add(MakePlatformStoreTag(platform));
        }
    }
}

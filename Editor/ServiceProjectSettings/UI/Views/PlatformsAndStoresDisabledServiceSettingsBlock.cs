using System;
using UnityEditor.Purchasing.UI.Presenters;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    class PlatformsAndStoresDisabledServiceSettingsBlock : PlatformsAndStoresServiceSettingsBlock
    {
        PlatformsStoreSettingsPresenter m_Presenter = new PlatformsStoreSettingsPresenter();

        protected override void PopulateStateSensitiveSections(VisualElement rootElement, VisualElement currentBuildTargetSection, VisualElement otherStoresSection)
        {
            currentBuildTargetSection.parent.Remove(currentBuildTargetSection);
            currentStoreSection.parent.Remove(currentStoreSection);
            otherStoresSection.parent.Remove(otherStoresSection);
        }

        protected override void PopulateSupportedStoresSection(VisualElement baseElement)
        {
            PopulateStores(baseElement, m_Presenter.GetAllStores());
        }
    }
}

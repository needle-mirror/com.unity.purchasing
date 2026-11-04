using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    class PlatformsAndStoresDisabledServiceSettingsBlock : PlatformsAndStoresServiceSettingsBlock
    {
        protected override void PopulateStateSensitiveSections(VisualElement rootElement, VisualElement currentBuildTargetSection, VisualElement otherStoresSection)
        {
            currentBuildTargetSection?.RemoveFromHierarchy();
            currentStoreSection?.RemoveFromHierarchy();
            otherStoresSection?.RemoveFromHierarchy();
        }

        protected override IEnumerable<string> GetStoresForState()
        {
            return GetAllStores();
        }

        protected override void FinalizeSetupPlatformAndStoresBlock()
        {
            //Nothing.
        }
    }
}

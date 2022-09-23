using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    internal abstract class BasePurchasingState : SimpleStateMachine<bool>.State
    {
        protected List<IPurchasingSettingsUIBlock> m_UIBlocks;

        protected BasePurchasingState(string stateName, SimpleStateMachine<bool> stateMachine)
            : base(stateName, stateMachine)
        {
            m_UIBlocks = new List<IPurchasingSettingsUIBlock>
            {
                PlatformsAndStoresServiceSettingsBlock.CreateStateSpecificBlock(IsEnabled()),
                new AnalyticsWarningSettingsBlock()
            };
        }

        internal List<VisualElement> GetStateUI()
        {
            return m_UIBlocks.Select(block => block.GetUIBlockElement()).ToList();
        }

        internal abstract bool IsEnabled();
    }
}

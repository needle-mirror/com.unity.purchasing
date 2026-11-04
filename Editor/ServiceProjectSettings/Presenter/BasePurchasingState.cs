using System.Collections.Generic;

namespace UnityEditor.Purchasing
{
    internal abstract class BasePurchasingState : SimpleStateMachine<bool>.State
    {
        protected BasePurchasingState(string stateName, SimpleStateMachine<bool> stateMachine)
            : base(stateName, stateMachine)
        {
            editorSetupBlock = PlatformsAndStoresServiceSettingsBlock.CreateStateSpecificBlock(IsEnabled());
        }

        internal IPurchasingSettingsUIBlock editorSetupBlock { get; }

        // Hosted in the "Cloud setup" tab, in this order: cloud blocks, then the "Not required for cloud" section, then the trailing blocks.
        internal List<IPurchasingSettingsUIBlock> cloudBlocks { get; } = new List<IPurchasingSettingsUIBlock>();
        internal List<IPurchasingSettingsUIBlock> notRequiredForCloudBlocks { get; } = new List<IPurchasingSettingsUIBlock>();
        internal List<IPurchasingSettingsUIBlock> trailingBlocks { get; } = new List<IPurchasingSettingsUIBlock>();

        internal abstract bool IsEnabled();
    }
}

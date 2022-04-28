namespace UnityEditor.Purchasing
{
    class PurchasingEnabledState : BasePurchasingState
    {
        internal const string k_StateNameEnabled = "EnabledState";

        public PurchasingEnabledState(SimpleStateMachine<bool> stateMachine)
            : base(k_StateNameEnabled, stateMachine)
        {
            m_UIBlocks.Add(new GooglePlayConfigurationSettingsBlock());
            m_UIBlocks.Add(new AppleConfigurationSettingsBlock());
            m_UIBlocks.Add(new IapCatalogServiceSettingsBlock());

            ModifyActionForEvent(false, HandleDisabling);
        }

        SimpleStateMachine<bool>.State HandleDisabling(bool raisedEvent)
        {
            return stateMachine.GetStateByName(PurchasingDisabledState.k_StateNameDisabled);
        }

        internal override bool IsEnabled() => true;
    }
}

namespace UnityEditor.Purchasing
{
    class PurchasingEnabledState : BasePurchasingState
    {
        internal const string k_StateNameEnabled = "EnabledState";

        public PurchasingEnabledState(SimpleStateMachine<bool> stateMachine)
            : base(k_StateNameEnabled, stateMachine)
        {
            cloudBlocks.Add(new GooglePlayConfigurationSettingsBlock());

            notRequiredForCloudBlocks.Add(new AppleConfigurationSettingsBlock());
            notRequiredForCloudBlocks.Add(new XboxConfigurationSettingsBlock());

            trailingBlocks.Add(new LegacyCatalogMigrationSettingsBlock());

            ModifyActionForEvent(false, HandleDisabling);
        }

        SimpleStateMachine<bool>.State HandleDisabling(bool raisedEvent)
        {
            return stateMachine.GetStateByName(PurchasingDisabledState.k_StateNameDisabled);
        }

        internal override bool IsEnabled() => true;
    }
}

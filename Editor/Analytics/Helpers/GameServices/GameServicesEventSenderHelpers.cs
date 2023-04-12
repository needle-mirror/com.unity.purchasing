namespace UnityEditor.Purchasing
{
    internal static class GameServicesEventSenderHelpers
    {
        internal static void SendTopMenuConfigure()
        {
            BuildAndSendEvent(GameServicesEventComponents.k_ComponentTopMenu, GameServicesEventActions.k_ActionConfigure);
        }

        internal static void SendTopMenuCreateIapButtonEvent()
        {
            BuildAndSendEvent(GameServicesEventComponents.k_ComponentTopMenu, GameServicesEventActions.k_ActionCreateIapButton_legacy);
        }

        internal static void SendTopMenuCreateCodelessIapButtonEvent()
        {
            BuildAndSendEvent(GameServicesEventComponents.k_ComponentTopMenu, GameServicesEventActions.k_ActionCreateIapButton);
        }

        internal static void SendTopMenuCreateIapListenerEvent()
        {
            BuildAndSendEvent(GameServicesEventComponents.k_ComponentTopMenu, GameServicesEventActions.k_ActionCreateIapListener);
        }

        internal static void SendTopMenuIapCatalogEvent()
        {
            BuildAndSendEvent(GameServicesEventComponents.k_ComponentTopMenu, GameServicesEventActions.k_ActionIapCatalog);
        }

        internal static void SendTopMenuReceiptValidationObfuscatorEvent()
        {
            BuildAndSendEvent(GameServicesEventComponents.k_ComponentTopMenu, GameServicesEventActions.k_ActionReceiptValidationObfuscator);
        }

        internal static void SendTopMenuSwitchStoreEvent()
        {
            BuildAndSendEvent(GameServicesEventComponents.k_ComponentTopMenu, GameServicesEventActions.k_ActionSwitchStore);
        }

        internal static void SendProjectSettingsValidatePublicKey()
        {
            BuildAndSendEvent(GameServicesEventComponents.k_ComponentProjectSettings, GameServicesEventActions.k_ActionValidatePublicKey);
        }

        static void BuildAndSendEvent(string component, string action)
        {
            var newEvent = new GenericEditorGameServiceEvent(component, action);
            PurchasingServiceAnalyticsSender.SendEvent(newEvent);
        }
    }
}

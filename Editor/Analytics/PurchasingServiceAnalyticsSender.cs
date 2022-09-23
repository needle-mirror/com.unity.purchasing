using System;

namespace UnityEditor.Purchasing
{
    [InitializeOnLoad]
    internal static class PurchasingServiceAnalyticsSender
    {
        static readonly IAnalyticsPackageKeyHolder m_Holder;

        static PurchasingServiceAnalyticsSender()
        {
#if SERVICES_SDK_CORE_ENABLED
            m_Holder = new GameServicesAnalyticsPackageKeyHolder();
#else
            m_Holder = new NonGameServicesAnalyticsPackageKeyHolder();
#endif
            RegisterEvents();
        }

        static void RegisterEvents()
        {
            PurchasingServiceAnalyticsRegistrar.RegisterEvent(SignatureDefinitions.k_GenericEditorSignature);
            PurchasingServiceAnalyticsRegistrar.RegisterEvent(SignatureDefinitions.k_EditorClickButtonSignature);
            PurchasingServiceAnalyticsRegistrar.RegisterEvent(SignatureDefinitions.k_EditorClickCheckboxSignature);
            PurchasingServiceAnalyticsRegistrar.RegisterEvent(SignatureDefinitions.k_EditorClickMenuItemSignature);
            PurchasingServiceAnalyticsRegistrar.RegisterEvent(SignatureDefinitions.k_EditorEditFieldSignature);
            PurchasingServiceAnalyticsRegistrar.RegisterEvent(SignatureDefinitions.k_EditorSelectDropdownSignature);
        }

        internal static void SendEvent(IEditorAnalyticsEvent eventToSend)
        {
            SendEventInternal(eventToSend.GetSignature(), eventToSend.CreateEventParams(GetPlatform(), m_Holder.GetPackageKey()));
        }


        static string GetPlatform()
        {
            return Enum.GetName(typeof(BuildTarget), EditorUserBuildSettings.activeBuildTarget);
        }

        static void SendEventInternal(EditorAnalyticsDataSignature eventSignature, object eventStruct)
        {
            EditorAnalytics.SendEventWithLimit(eventSignature.eventName, eventStruct, eventSignature.version);
        }
    }
}

using Unity.Services.Core;

namespace UnityEngine.Purchasing
{
    class UnityServicesInitializationChecker : IUnityServicesInitializationChecker
    {
        const string UgsUninitializedMessage =
            "<b>Unity In-App Purchasing</b> requires <b>Unity Gaming Services</b> to have been initialized before use.\n" +
            "- Find out how to initialize <b>Unity Gaming Services</b> by following the documentation <i>https://docs.unity.com/ugs-overview/services-core-api.html#InitializationExample</i>\n" +
            "or download the <i>06 Initialize Gaming Services</i> sample from <i>Package Manager > In-App Purchasing > Samples</i>.\n" +
            "- If you are using the codeless API, you may want to enable the enable <b>Unity Gaming Services</b> automatic initialization " +
            "by checking the <b>Automatically initialize Unity Gaming Services</b> checkbox at the bottom of the <b>IAP Catalog</b> window";

        ILogger m_Logger;

        public UnityServicesInitializationChecker(ILogger logger)
        {
            m_Logger = logger;
        }

        public void CheckAndLogWarning()
        {
            if (IsUninitialized())
            {
                LogWarning();
            }
        }

        bool IsUninitialized()
        {
            return UnityServices.State == ServicesInitializationState.Uninitialized;
        }

        void LogWarning()
        {
            m_Logger.LogIAPWarning(UgsUninitializedMessage);
        }
    }
}

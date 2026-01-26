#nullable enable

using Uniject;
using UnityEngine.Purchasing.GoogleBilling.Models;
using UnityEngine.Purchasing.Telemetry;

namespace UnityEngine.Purchasing.GoogleBilling
{
    internal class BillingClientFactory
    {
        static BillingClientFactory? s_Instance;

        BillingClientFactory()
        {
        }

        internal static BillingClientFactory Instance()
        {
            if (s_Instance == null)
            {
                var logger = Debug.unityLogger;
                s_Instance = new BillingClientFactory();
            }

            return s_Instance;
        }

        internal ExternalBillingProgramClientInternal CreateExternalBillingProgramClient()
        {
            return new ExternalBillingProgramClientInternal();
        }
    }
}

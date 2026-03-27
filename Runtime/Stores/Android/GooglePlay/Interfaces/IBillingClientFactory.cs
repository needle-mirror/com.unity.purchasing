using UnityEngine.Purchasing.GoogleBilling.Interfaces;

namespace UnityEngine.Purchasing
{
    internal interface IBillingClientFactory
    {
        IExternalBillingProgramClientInternal CreateExternalBillingProgramClient();
    }
}

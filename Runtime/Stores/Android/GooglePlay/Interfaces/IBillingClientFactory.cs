using Stores.Android.GooglePlay.AAR.Models;
using UnityEngine.Purchasing.GoogleBilling.Interfaces;

namespace UnityEngine.Purchasing
{
    internal interface IBillingClientFactory
    {
        IExternalBillingProgramClientInternal CreateExternalBillingProgramClient();
        IExternalBillingProgramClientInternal CreateExternalBillingProgramClient(BillingProgram billingProgram);
    }
}

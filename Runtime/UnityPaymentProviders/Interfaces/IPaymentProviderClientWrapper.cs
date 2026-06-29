using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using UnityEngine.Purchasing.PaymentProviderService;

namespace UnityEngine.Purchasing.Stores
{
    internal interface IPaymentProviderClientWrapper
    {
        bool PaymentProviderClientIsAvailable { get; }
        IPaymentProviderService GetPaymentProviderService();
        void CreatePaymentProviderService(IAccessToken accessToken, IEnvironmentId environmentId, ICloudProjectId cloudProjectId, string baseUrl = null);
    }
}

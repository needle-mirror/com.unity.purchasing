using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using UnityEngine.Purchasing.PaymentProviderService;
using UnityEngine.Purchasing.PaymentProviderService.Http;

namespace UnityEngine.Purchasing.Stores
{
    internal class PaymentProviderClientWrapper : IPaymentProviderClientWrapper
    {

        IPaymentProviderService m_PaymentProviderService;
        public bool PaymentProviderClientIsAvailable => m_PaymentProviderService != null;

        public IPaymentProviderService GetPaymentProviderService()
        {
            return m_PaymentProviderService;
        }

        public void CreatePaymentProviderService(IAccessToken accessToken, IEnvironmentId environmentId, ICloudProjectId cloudProjectId, string baseUrl = null)
        {
            m_PaymentProviderService ??= new InternalPaymentProviderService(accessToken, environmentId, cloudProjectId, baseUrl);
        }
    }
}

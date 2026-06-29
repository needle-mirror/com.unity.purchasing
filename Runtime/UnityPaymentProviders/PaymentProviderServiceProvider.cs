using UnityEngine.Purchasing.Stores;

namespace UnityEngine.Purchasing.PaymentProviderService
{
    static class PaymentProviderServiceProvider
    {
        public static IPaymentProviderClientWrapper Instance()
        {
            return s_Instance ??= new PaymentProviderClientWrapper();
        }

        static IPaymentProviderClientWrapper s_Instance;
    }
}

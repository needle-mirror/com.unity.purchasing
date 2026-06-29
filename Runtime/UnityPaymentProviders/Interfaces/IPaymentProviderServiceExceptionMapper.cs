using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.PaymentProviderService
{
    internal interface IPaymentProviderServiceExceptionMapper
    {
        Task<T> InvokeAndMapServiceExceptions<T>(Func<Task<T>> caller);
    }
}

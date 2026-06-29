using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.WebshopService
{
    internal interface IWebshopServiceExceptionMapper
    {
        Task<T> InvokeAndMapServiceExceptions<T>(Func<Task<T>> caller);
    }
}

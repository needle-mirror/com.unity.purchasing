using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.LiveContentAdapterService
{
    internal interface ILiveContentAdapterServiceExceptionMapper
    {
        Task<T> InvokeAndMapServiceExceptions<T>(Func<Task<T>> caller);
    }
}

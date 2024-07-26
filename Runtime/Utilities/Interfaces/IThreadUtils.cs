using System;
using System.Threading.Tasks;

namespace Uniject
{
    interface IThreadUtils
    {
        bool IsRunningOnMainThread { get; }
        Task PostAsync(Action action);
        Task PostAsync(Action<object> action, object state);
        Task<T> PostAsync<T>(Func<T> action);
        Task<T> PostAsync<T>(Func<object, T> action, object state);
    }
}

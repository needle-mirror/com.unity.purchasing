using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

namespace Uniject
{
    [Preserve]
    class UnityThreadUtils : IThreadUtils
    {
        static int s_UnityThreadId;

        static TaskScheduler UnityThreadScheduler;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void CaptureUnityThreadInfo()
        {
            s_UnityThreadId = Thread.CurrentThread.ManagedThreadId;
            UnityThreadScheduler = TaskScheduler.FromCurrentSynchronizationContext();
        }

        public bool IsRunningOnMainThread => Thread.CurrentThread.ManagedThreadId == s_UnityThreadId;

        public Task PostAsync(Action action)
        {
            return Task.Factory.StartNew(
                action, CancellationToken.None, TaskCreationOptions.None, UnityThreadScheduler);
        }

        public Task PostAsync(Action<object> action, object state)
        {
            return Task.Factory.StartNew(
                action, state, CancellationToken.None, TaskCreationOptions.None,
                UnityThreadScheduler);
        }

        public Task<T> PostAsync<T>(Func<T> action)
        {
            return Task<T>.Factory.StartNew(
                action, CancellationToken.None, TaskCreationOptions.None, UnityThreadScheduler);
        }

        public Task<T> PostAsync<T>(Func<object, T> action, object state)
        {
            return Task<T>.Factory.StartNew(
                action, state, CancellationToken.None, TaskCreationOptions.None,
                UnityThreadScheduler);
        }
    }

}

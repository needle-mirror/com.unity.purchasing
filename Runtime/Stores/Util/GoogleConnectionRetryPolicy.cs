#nullable enable

using System;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Stores.Util;

namespace UnityEngine.Purchasing
{
    class GoogleConnectionRetryPolicy : IRetryPolicy
    {
        readonly int m_BaseRetryDelay;
        readonly int m_MaxRetryDelay;
        readonly int m_ExponentialFactor;

        public GoogleConnectionRetryPolicy(int baseRetryDelay = 2000, int maxRetryDelay = 30 * 1000, int exponentialFactor = 2)
        {
            m_BaseRetryDelay = baseRetryDelay;
            m_MaxRetryDelay = maxRetryDelay;
            m_ExponentialFactor = exponentialFactor;
        }

        public void Invoke(Action<Action> actionToTry, Action? onRetryAction)
        {
            int retryAttempts = 0;
            var currentRetryDelay = m_BaseRetryDelay;
            WaitAndRetry();

            async void WaitAndRetry()
            {
                await Task.Delay(currentRetryDelay);
                currentRetryDelay = AdjustDelay(currentRetryDelay);
                actionToTry(WaitAndRetry);
                onRetryAction?.Invoke();
                retryAttempts++;
            }
        }

        int AdjustDelay(int delay)
        {
            return Math.Min(m_MaxRetryDelay, delay * m_ExponentialFactor);
        }
    }
}

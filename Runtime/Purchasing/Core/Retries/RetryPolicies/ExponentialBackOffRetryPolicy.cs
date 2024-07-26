#nullable enable

using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    public class ExponentialBackOffRetryPolicy : IRetryPolicy
    {
        readonly int m_BaseRetryDelay;
        readonly int m_MaxRetryDelay;
        readonly float m_ExponentialFactor;
        readonly int m_MaxNumberOfRetriesBeforeCeiling;
        readonly IAsyncDelayer m_Delayer;

        public ExponentialBackOffRetryPolicy(int baseRetryDelay = 1000, int maxRetryDelay = 30 * 1000,
            float exponentialFactor = 2)
        {
            ValidateArguments(baseRetryDelay, maxRetryDelay, exponentialFactor);

            m_BaseRetryDelay = baseRetryDelay;
            m_MaxRetryDelay = maxRetryDelay;
            m_ExponentialFactor = exponentialFactor;

            //We calculate this to prevent overflows
            m_MaxNumberOfRetriesBeforeCeiling =
                CalculateMaxNumberOfRetriesBeforeCeiling(baseRetryDelay, maxRetryDelay, exponentialFactor);

            m_Delayer = new AsyncDelayer();
        }

        static void ValidateArguments(int baseRetryDelay, int maxRetryDelay, float exponentialFactor)
        {
            if (baseRetryDelay <= 0)
            {
                throw new ArgumentException("Base retry delay must be greater than 0.");
            }

            if (maxRetryDelay <= 0)
            {
                throw new ArgumentException("Maximum retry delay must be greater than 0.");
            }

            if (exponentialFactor <= 0)
            {
                throw new ArgumentException("The exponential factor must be greater than 0.");
            }
        }

        int CalculateMaxNumberOfRetriesBeforeCeiling(int baseRetryDelay, int maxRetryDelay, float exponentialFactor)
        {
            return (int)Mathf.Log((float)maxRetryDelay / baseRetryDelay, exponentialFactor);
        }

        internal ExponentialBackOffRetryPolicy(IAsyncDelayer delayer, int baseRetryDelay = 1000, int maxRetryDelay = 30 * 1000,
            float exponentialFactor = 2) : this(baseRetryDelay, maxRetryDelay, exponentialFactor)
        {
            m_Delayer = delayer;
        }

        public virtual async Task<bool> ShouldRetry(IRetryPolicyInformation info)
        {
            var currentRetryDelay = AdjustDelay(info.NumberOfAttempts - 1);
            await m_Delayer.Delay(currentRetryDelay);
            return true;
        }

        int AdjustDelay(int numberOfRetries)
        {
            if (HasHitMaxRetryDelay(numberOfRetries))
            {
                return m_MaxRetryDelay;
            }

            return (int)(m_BaseRetryDelay * Mathf.Pow(m_ExponentialFactor, numberOfRetries));
        }

        bool HasHitMaxRetryDelay(int numberOfRetries)
        {
            return numberOfRetries > m_MaxNumberOfRetriesBeforeCeiling && m_ExponentialFactor > 1;
        }
    }
}

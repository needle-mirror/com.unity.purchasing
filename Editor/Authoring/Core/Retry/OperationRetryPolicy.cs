using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Retry
{
    /// <summary>
    /// Executes an async operation with exponential backoff retry.
    /// Supports jitter, Retry-After header extraction, and cancellation.
    /// </summary>
    internal sealed class OperationRetryPolicy
    {
        [ThreadStatic]
        static Random s_Random;

        int m_MaxRetries = 4;
        int m_BaseDelayMs = 1000;
        int m_MaxDelayMs = 8000;
        int m_MaxJitterMs = 250;
        int m_MaxRetryAfterSeconds = 30;
        Func<TimeSpan, CancellationToken, Task> m_DelayFunc = Task.Delay;
        Func<Exception, bool> m_RetryCondition;
        Func<Exception, TimeSpan?> m_RetryAfterExtractor;

        OperationRetryPolicy() {}

        public static OperationRetryPolicy Create() => new OperationRetryPolicy();

        /// <summary>Maximum number of retries after the initial attempt. Must be >= 0.</summary>
        public OperationRetryPolicy WithMaxRetries(int maxRetries)
        {
            if (maxRetries < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxRetries), "Must be >= 0.");
            }

            m_MaxRetries = maxRetries;
            return this;
        }

        /// <summary>Base and maximum delay in milliseconds for exponential backoff.</summary>
        public OperationRetryPolicy WithBackoff(int baseDelayMs, int maxDelayMs)
        {
            if (baseDelayMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseDelayMs), "Must be >= 0.");
            }

            if (maxDelayMs < baseDelayMs)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDelayMs), "Must be >= baseDelayMs.");
            }

            m_BaseDelayMs = baseDelayMs;
            m_MaxDelayMs = maxDelayMs;
            return this;
        }

        /// <summary>Maximum random jitter in milliseconds added to each delay. Must be >= 0.</summary>
        public OperationRetryPolicy WithJitter(int maxJitterMs)
        {
            if (maxJitterMs < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxJitterMs), "Must be >= 0.");
            }

            m_MaxJitterMs = maxJitterMs;
            return this;
        }

        /// <summary>Cap on server-supplied Retry-After values, in seconds. Must be >= 0.</summary>
        public OperationRetryPolicy WithMaxRetryAfter(int maxSeconds)
        {
            if (maxSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSeconds), "Must be >= 0.");
            }

            m_MaxRetryAfterSeconds = maxSeconds;
            return this;
        }

        /// <summary>
        /// Override for the inter-attempt delay. Defaults to <see cref="Task.Delay(TimeSpan,CancellationToken)"/>.
        /// Inject a no-op in unit tests to avoid real waits.
        /// </summary>
        public OperationRetryPolicy WithDelayFunc(Func<TimeSpan, CancellationToken, Task> delayFunc)
        {
            m_DelayFunc = delayFunc ?? throw new ArgumentNullException(nameof(delayFunc));
            return this;
        }

        /// <summary>
        /// Predicate that decides whether a given exception should trigger a retry.
        /// If not set, no exception is retried.
        /// </summary>
        /// <remarks>
        /// For HTTP calls: prefer retrying only 429 (Too Many Requests) with the Retry-After header.
        /// Retrying 5xx errors can overwhelm an already degraded backend — consider whether the
        /// operation is idempotent and whether retries actually help before enabling them.
        /// </remarks>
        public OperationRetryPolicy WithRetryCondition(Func<Exception, bool> shouldRetry)
        {
            m_RetryCondition = shouldRetry;
            return this;
        }

        /// <summary>
        /// Optional extractor that reads a server-specified delay from the exception.
        /// When it returns a non-null TimeSpan, that value is used instead of the
        /// computed exponential backoff (capped at MaxRetryAfter).
        /// </summary>
        public OperationRetryPolicy WithRetryAfterExtractor(Func<Exception, TimeSpan?> extractor)
        {
            m_RetryAfterExtractor = extractor;
            return this;
        }

        /// <summary>
        /// Execute an async operation with retry on failure.
        /// </summary>
        public async Task ExecuteAsync(Func<Task> operation, CancellationToken ct)
        {
            await ExecuteAsync<object>(async () =>
            {
                await operation();
                return null;
            }, ct);
        }

        /// <summary>
        /// Execute an async operation with retry on failure, returning the result on success.
        /// </summary>
        public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken ct)
        {
            for (int attempt = 0; attempt <= m_MaxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    return await operation();
                }
                catch (Exception e) when (!(e is OperationCanceledException))
                {
                    if (m_RetryCondition == null || !m_RetryCondition(e))
                    {
                        throw;
                    }

                    if (attempt == m_MaxRetries)
                    {
                        throw;
                    }

                    var delay = ComputeDelay(attempt, m_RetryAfterExtractor?.Invoke(e));
                    await m_DelayFunc(delay, ct);
                }
            }

            throw new InvalidOperationException(
                "Unreachable: retry loop completed without result or exception.");
        }

        /// <summary>
        /// Result-based overload: retries when <paramref name="shouldRetry"/> returns true
        /// for the result, without requiring exceptions for flow control.
        /// Returns the last result when retries are exhausted.
        /// </summary>
        public async Task<T> ExecuteAsync<T>(
            Func<Task<T>> operation,
            Func<T, bool> shouldRetry,
            CancellationToken ct,
            Func<T, TimeSpan?> retryAfterExtractor = null)
        {
            T lastResult = default;

            for (int attempt = 0; attempt <= m_MaxRetries; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                lastResult = await operation();

                if (!shouldRetry(lastResult))
                {
                    return lastResult;
                }

                if (attempt == m_MaxRetries)
                {
                    return lastResult;
                }

                var delay = ComputeDelay(attempt, retryAfterExtractor?.Invoke(lastResult));
                await m_DelayFunc(delay, ct);
            }

            return lastResult;
        }

        TimeSpan ComputeDelay(int attempt, TimeSpan? serverRetryAfter = null)
        {
            if (serverRetryAfter.HasValue)
            {
                var seconds = Math.Max(0, Math.Min(serverRetryAfter.Value.TotalSeconds, m_MaxRetryAfterSeconds));
                return TimeSpan.FromSeconds(seconds);
            }

            var shift = Math.Min(attempt, 30);
            long backoffMs = Math.Min((long)m_BaseDelayMs * (1L << shift), m_MaxDelayMs);
            long jitterMs = 0;
            if (m_MaxJitterMs > 0)
            {
                if (s_Random == null)
                {
                    s_Random = new Random();
                }

                jitterMs = s_Random.Next(0, m_MaxJitterMs);
            }

            var totalMs = Math.Min(backoffMs + jitterMs, int.MaxValue);
            return TimeSpan.FromMilliseconds(totalMs);
        }
    }
}

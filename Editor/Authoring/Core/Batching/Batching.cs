using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Batching
{
    /// <summary>Utility class for executing async operations in batches with bounded concurrency.</summary>
    internal static class Batching
    {
        const int k_BatchSize = 10;
        const double k_SecondsDelay = 1;

        const string k_BatchingExceptionMessage =
            "One or more exceptions were thrown during the batching execution. See inner exceptions.";

        /// <summary>
        /// Execute task factories in batches of <paramref name="batchSize"/> with a delay
        /// between each batch to stay within gateway rate limits.
        /// </summary>
        /// <param name="tasks">Factories that produce the tasks to execute. Each factory is invoked
        /// only when its batch starts.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <param name="batchSize">Number of tasks per batch.</param>
        /// <param name="secondsDelay">Delay in seconds between batches.</param>
        /// <exception cref="AggregateException">Thrown when one or more task factories throw.</exception>
        public static async Task ExecuteInBatchesAsync(
            IEnumerable<Func<Task>> tasks,
            CancellationToken cancellationToken,
            int batchSize = k_BatchSize,
            double secondsDelay = k_SecondsDelay)
        {
            if (batchSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(batchSize), "Must be greater than zero.");
            }

            if (secondsDelay < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(secondsDelay), "Must not be negative.");
            }

            var exceptions = new ConcurrentQueue<Exception>();
            var batch = new List<Task>(batchSize);

            using var enumerator = tasks.GetEnumerator();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                batch.Clear();
                var allDone = false;

                for (int i = 0; i < batchSize; i++)
                {
                    if (!enumerator.MoveNext())
                    {
                        allDone = true;
                        break;
                    }

                    batch.Add(RunSafe(enumerator.Current, exceptions));
                }

                if (batch.Count == 0)
                {
                    break;
                }

                await Task.WhenAll(batch);

                if (allDone || cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(secondsDelay), cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            if (!exceptions.IsEmpty)
            {
                throw new AggregateException(k_BatchingExceptionMessage, exceptions.ToList());
            }
        }

        /// <summary>
        /// Convenience overload that creates task factories from a collection of items
        /// and an async operation to apply to each item.
        /// </summary>
        /// <param name="items">The items to process.</param>
        /// <param name="operation">The async operation to apply to each item.</param>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        /// <param name="batchSize">Number of tasks per batch.</param>
        /// <param name="secondsDelay">Delay in seconds between batches.</param>
        public static Task ExecuteInBatchesAsync<T>(
            IEnumerable<T> items,
            Func<T, Task> operation,
            CancellationToken cancellationToken,
            int batchSize = k_BatchSize,
            double secondsDelay = k_SecondsDelay)
        {
            return ExecuteInBatchesAsync(
                items.Select(item => (Func<Task>)(() => operation(item))),
                cancellationToken,
                batchSize,
                secondsDelay);
        }

        static async Task RunSafe(
            Func<Task> factory,
            ConcurrentQueue<Exception> exceptions)
        {
            try
            {
                await factory();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                exceptions.Enqueue(e);
            }
        }
    }
}

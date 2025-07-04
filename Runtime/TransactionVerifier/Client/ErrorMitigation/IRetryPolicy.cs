//-----------------------------------------------------------------------------
// <auto-generated>
//     This file was generated by the C# SDK Code Generator.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//-----------------------------------------------------------------------------


using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.TransactionVerifier.ErrorMitigation
{
    /// <summary>
    /// Retry Policy Provider class for defining how exponential backoff and
    /// retry behaviour should work.
    /// </summary>
    internal interface IRetryPolicyProvider
    {
        /// <summary>
        /// Constructs a RetryPolicy based on the passed in operations type.
        /// </summary>
        /// <param name="operation">
        /// The operation we are executing as a function that returns the type
        /// of operation as a `Task` and has an `int` parameter representing
        /// which retry attempt we are on.
        /// </param>
        /// <typeparam name="T">The type of the operation.</typeparam>
        /// <returns>A IRetryPolicy</returns>
        IRetryPolicy<T> ForOperation<T>(Func<int, Task<T>> operation);

        /// <summary>
        /// Constructs a RetryPolicy based on the passed in operations type.
        /// </summary>
        /// <param name="operation">
        /// The operation we are executing as a function that returns the type
        /// of operation as a `Task`.
        /// </param>
        /// <typeparam name="T">The type of the operation.</typeparam>
        /// <returns>A IRetryPolicy</returns>
        IRetryPolicy<T> ForOperation<T>(Func<Task<T>> operation);
    }

    /// <summary>
    /// Retry Policy class for defining how exponential backoff and retry
    /// behaviour should work.
    /// </summary>
    /// <typeparam name="T">The type of the operation.</typeparam>
    internal interface IRetryPolicy<T>
    {
        /// <summary>
        /// Sets the Jitter Magnitude to help prevent a service from being
        /// overloaded with retry requests at regular intervals.
        /// </summary>
        /// <param name="magnitude">The magnitude to use.</param>
        /// <returns>An IRetryPolicy{T}.</returns>
        IRetryPolicy<T> WithJitterMagnitude(float magnitude);

        /// <summary>
        /// Sets a Delay Scale that is used to calculate the time between each retry.
        /// </summary>
        /// <param name="scale">The delay scale to use.</param>
        /// <returns>An IRetryPolicy{T}.</returns>
        IRetryPolicy<T> WithDelayScale(float scale);

        /// <summary>
        /// Sets a Max Delay time between each retry.
        /// </summary>
        /// <param name="time">The maximum delay time.</param>
        /// <returns>An IRetryPolicy{T}.</returns>
        IRetryPolicy<T> WithMaxDelayTime(float time);

        /// <summary>
        /// Sets a Retry Condition method that returns whether or not there
        /// should be another retry.
        /// </summary>
        /// <param name="shouldRetry">A function that takes an expected T and returns a bool.</param>
        /// <returns>An IRetryPolicy{T}.</returns>
        IRetryPolicy<T> WithRetryCondition(Func<T, Task<bool>> shouldRetry);

        /// <summary>
        /// Sets the maximum number of retries.
        /// </summary>
        /// <param name="amount">The max amount of retries.</param>
        /// <returns>An IRetryPolicy{T}.</returns>
        IRetryPolicy<T> UptoMaximumRetries(uint amount);

        /// <summary>
        /// Adds an exception type that, if thrown, should trigger a retry.
        /// </summary>
        /// <typeparam name="TException">The exception type to retry on.</typeparam>
        /// <returns>An IRetryPolicy{T}.</returns>
        IRetryPolicy<T> HandleException<TException>() where TException : Exception;

        /// <summary>
        /// Adds an exception type that and a condition, if thrown, should trigger a retry.
        /// </summary>
        /// <typeparam name="TException">The exception type to retry on.</typeparam>
        /// <param name="condition">A condition function  for specifying additional behaviour when checking if we should retry with this exception type.</param>
        /// <returns>An IRetryPolicy{T}.</returns>
        IRetryPolicy<T> HandleException<TException>(Func<TException, bool> condition) where TException : Exception;

        /// <summary>
        /// Runs the specified operation async and will retry if the shouldRetry
        /// method returns true and the number of retries hasn't reached the
        /// maximum based on the provided RetryPolicyConfig.
        /// </summary>
        /// <param name="retryPolicyConfig">A Retry Policy Configuration that
        /// specifies how often and what way the operation should be retried.</param>
        /// <returns>A Task{T}.</returns>
        Task<T> RunAsync(RetryPolicyConfig retryPolicyConfig = null);
    }
}

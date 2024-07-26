#nullable enable

using System;
using System.Threading.Tasks;
using Uniject;

namespace UnityEngine.Purchasing
{
    class RetryRequest : IRetryRequest
    {
        float m_StartTime;
        int m_NumberOfAttempts = 0;

        readonly Action m_Request;
        readonly IRetryPolicy m_RetryPolicy;
        readonly IUtil m_Util;


        public RetryRequest(Action request, IRetryPolicy retryPolicy, IUtil util)
        {
            m_Request = request;
            m_RetryPolicy = retryPolicy;
            m_Util = util;
        }

        public Task<bool> Invoke()
        {
            return RunTaskOnMainThread(InvokeFromMainThread);
        }

        public Task<bool> RunTaskOnMainThread(Func<Task<bool>> function)
        {
            var completionSource = new TaskCompletionSource<bool>();

            async void RunRequestAndSetResult()
            {
                var result = await function();
                completionSource.SetResult(result);
            }

            m_Util.RunOnMainThread(RunRequestAndSetResult);
            return completionSource.Task;
        }


        async Task<bool> InvokeFromMainThread()
        {
            if (m_NumberOfAttempts != 0)
            {
                return await RetryFromMainThread();
            }

            FirstTry();
            return true;
        }

        void FirstTry()
        {
            m_StartTime = Time.time;
            m_NumberOfAttempts++;
            m_Request();
        }

        public async Task<bool> Retry(IRetryableRequestFailureDescription requestFailureDescription)
        {
            if (requestFailureDescription.IsRetryable)
            {
                return await RunTaskOnMainThread(RetryFromMainThread);
            }

            return false;
        }

        async Task<bool> RetryFromMainThread()
        {
            var retryInfo = new RetryPolicyInformation(m_NumberOfAttempts, Time.time - m_StartTime);

            if (await m_RetryPolicy.ShouldRetry(retryInfo))
            {
                m_NumberOfAttempts++;
                m_Request();
                return true;
            }

            return false;
        }
    }
}

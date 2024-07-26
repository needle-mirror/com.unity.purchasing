#nullable enable

using System;
using Uniject;

namespace UnityEngine.Purchasing
{
    class RetryService : IRetryService
    {
        readonly IUtil m_Util;

        internal RetryService(IUtil util)
        {
            m_Util = util;
        }

        public IRetryRequest CreateRequest(Action request, IRetryPolicy retryPolicy)
        {
            return new RetryRequest(request, retryPolicy, m_Util);
        }
    }
}

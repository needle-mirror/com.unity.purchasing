#nullable enable

using System;

namespace UnityEngine.Purchasing
{
    interface IRetryService
    {
        IRetryRequest CreateRequest(Action request, IRetryPolicy retryPolicy);
    }
}

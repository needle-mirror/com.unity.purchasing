#nullable enable

namespace UnityEngine.Purchasing
{
    interface IRetryableRequestFailureDescription
    {
        bool IsRetryable { get; }
    }
}

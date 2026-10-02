#nullable enable
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    internal interface IFirebaseAnalyticsClient
    {
        // timedOut: the request was abandoned after a timeout, rather than Firebase reporting no session.
        Task<(string? sessionId, bool timedOut)> FetchSessionIdAsync();
        Task<string?> FetchAppInstanceIdAsync();
        Task<string?> FetchAppIdAsync();
    }
}

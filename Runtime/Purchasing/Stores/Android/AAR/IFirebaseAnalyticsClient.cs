#nullable enable
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    internal interface IFirebaseAnalyticsClient
    {
        Task<string?> FetchSessionIdAsync();
        Task<string?> FetchAppInstanceIdAsync();
        Task<string?> FetchAppIdAsync();
    }
}

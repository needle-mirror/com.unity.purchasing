#nullable enable

using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    internal interface IGoogleAdvertisingIdClient
    {
        Task<string?> FetchGaidAsync();
    }
}

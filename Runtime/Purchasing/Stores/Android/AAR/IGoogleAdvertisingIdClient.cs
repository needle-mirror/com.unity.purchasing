#nullable enable

namespace UnityEngine.Purchasing
{
    internal interface IGoogleAdvertisingIdClient
    {
        string? FetchGaid();
    }
}

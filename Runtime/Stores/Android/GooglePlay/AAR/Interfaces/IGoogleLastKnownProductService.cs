#nullable enable

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGoogleLastKnownProductService
    {
        string? LastKnownOldProductId { get; set; }
        string? LastKnownProductId { get; set; }
        GooglePlayProrationMode? LastKnownProrationMode { get; set; }
    }
}

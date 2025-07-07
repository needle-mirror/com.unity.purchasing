#nullable enable

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGoogleLastKnownProductService
    {
        string? LastKnownOldProductId { get; set; }
        string? LastKnownProductId { get; set; }
        GooglePlayReplacementMode? LastKnownReplacementMode { get; set; }
    }
}

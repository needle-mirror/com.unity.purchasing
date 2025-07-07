#nullable enable

using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    class GoogleLastKnownProductService : IGoogleLastKnownProductService
    {
        public string? LastKnownOldProductId { get; set; }
        public string? LastKnownProductId { get; set; }

        public GooglePlayReplacementMode? LastKnownReplacementMode { get; set; } =
            GooglePlayReplacementMode.UnknownReplacementMode;
    }
}

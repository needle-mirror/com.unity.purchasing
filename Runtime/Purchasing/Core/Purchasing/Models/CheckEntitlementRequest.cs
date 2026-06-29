using System;

namespace UnityEngine.Purchasing
{
    internal class CheckEntitlementRequest
    {
        internal Product ProductToCheck { get; }
        internal Action<Entitlement> OnChecked { get; }

        // A product may carry multiple listings under the same uSku; we send one store call per
        // listing and aggregate the responses. RemainingListings counts callbacks still expected;
        // BestStatus tracks the highest entitlement seen so far (enum is ordered so max wins:
        // FullyEntitled > EntitledButNotFinished > EntitledUntilConsumed > NotEntitled > Unknown).
        internal int RemainingListings { get; set; }
        internal EntitlementStatus BestStatus { get; set; } = EntitlementStatus.Unknown;
        internal string LastMessage { get; set; }

        internal CheckEntitlementRequest(Product product, Action<Entitlement> onChecked)
        {
            ProductToCheck = product;
            OnChecked = onChecked;
        }
    }
}

using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Encapsulate a complete list of Order.cs with ConfirmedOrders and PendingOrders.
    /// Used when fetching purchases.
    /// </summary>
    /// <param name="confirmedOrders">IReadOnlyList of ConfirmedOrders</param>
    /// <param name="pendingOrder">IReadOnlyList of PendingOrder</param>
    /// <param name="deferredOrders">IReadOnlyList of DeferredOrders</param>
    public class Orders
    {
        public Orders(IReadOnlyList<ConfirmedOrder> confirmedOrders, IReadOnlyList<PendingOrder> pendingOrders, IReadOnlyList<DeferredOrder> deferredOrders)
        {
            ConfirmedOrders = confirmedOrders;
            PendingOrders = pendingOrders;
            DeferredOrders = deferredOrders;
        }

        public IReadOnlyList<ConfirmedOrder> ConfirmedOrders { get; }
        public IReadOnlyList<PendingOrder> PendingOrders { get; }
        public IReadOnlyList<DeferredOrder> DeferredOrders { get; }
    }
}

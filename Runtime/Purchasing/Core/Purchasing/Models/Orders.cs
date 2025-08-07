using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Encapsulate a complete list of Orders with ConfirmedOrders, PendingOrders and DeferredOrders.
    /// Used when fetching purchases.
    /// </summary>
    public class Orders
    {
        /// <summary>
        /// Encapsulate a complete list of Orders with ConfirmedOrders, PendingOrders and DeferredOrders.
        /// </summary>
        /// <param name="confirmedOrders">IReadOnlyList of ConfirmedOrders</param>
        /// <param name="pendingOrders">IReadOnlyList of PendingOrder</param>
        /// <param name="deferredOrders">IReadOnlyList of DeferredOrders</param>
        public Orders(IReadOnlyList<ConfirmedOrder> confirmedOrders, IReadOnlyList<PendingOrder> pendingOrders, IReadOnlyList<DeferredOrder> deferredOrders)
        {
            ConfirmedOrders = confirmedOrders;
            PendingOrders = pendingOrders;
            DeferredOrders = deferredOrders;
        }

        /// <summary>
        /// Gets the list of confirmed orders.
        /// </summary>
        public IReadOnlyList<ConfirmedOrder> ConfirmedOrders { get; }
        /// <summary>
        /// Gets the list of pending orders.
        /// </summary>
        public IReadOnlyList<PendingOrder> PendingOrders { get; }
        /// <summary>
        /// Gets the list of deferred orders.
        /// </summary>
        public IReadOnlyList<DeferredOrder> DeferredOrders { get; }
    }
}

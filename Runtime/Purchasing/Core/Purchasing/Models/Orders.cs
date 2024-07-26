using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Encapsulate a complete list of Order.cs with ConfirmedOrders and PendingOrders.
    /// Used when fetching purchases.
    /// </summary>
    /// <param name="confirmedOrders">IReadOnlyList of ConfirmedOrders</param>
    /// <param name="pendingOrder">IReadOnlyList of PendingOrder</param>
    public class Orders
    {
        public Orders(IReadOnlyList<ConfirmedOrder> confirmedOrders, IReadOnlyList<PendingOrder> pendingOrders)
        {
            ConfirmedOrders = confirmedOrders;
            PendingOrders = pendingOrders;
        }

        public IReadOnlyList<ConfirmedOrder> ConfirmedOrders { get; }
        public IReadOnlyList<PendingOrder> PendingOrders { get; }
    }
}

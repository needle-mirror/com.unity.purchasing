namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The entitlement status of a product
    /// </summary>
    public enum EntitlementStatus
    {
        /// <summary>
        /// Entitlement Status is not known.
        /// </summary>
        Unknown,
        /// <summary>
        /// The Product is not entitled.
        /// </summary>
        NotEntitled,
        /// <summary>
        /// The Consumable is entitled until it is consumed and awarded.
        /// Often the case for crashes mid-transaction or consumables obtained via voucher codes
        /// If it has an associated PendingOrder, it can be consumed via ConfirmPurchase
        /// </summary>
        EntitledUntilConsumed,
        /// <summary>
        /// The Non-Consumable or Subscription has a valid entitlement, but its transaction has not been fully completed.
        /// If it has an associated PendingOrder, it can be completed via ConfirmPurchase
        /// </summary>
        EntitledButNotFinished,
        /// <summary>
        /// The Non-Consumable or Subscription has a valid entitlement with a completed order.
        /// It should have an associated ConfirmedOrder.
        /// </summary>
        FullyEntitled,
    }
}

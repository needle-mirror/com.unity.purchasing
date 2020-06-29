namespace UnityEngine.Purchasing
{
    public enum ProductType
    {
        /// <summary>
        /// Consumables may be purchased more than once.
        ///
        /// Purchase history is not typically retained by store
        /// systems once consumed.
        /// </summary>
        Consumable,

        /// <summary>
        /// Non consumables cannot be repurchased and are owned indefinitely.
        /// </summary>
        NonConsumable,

        /// <summary>
        /// Subscriptions have a finite window of validity.
        /// </summary>
        Subscription
    }
}

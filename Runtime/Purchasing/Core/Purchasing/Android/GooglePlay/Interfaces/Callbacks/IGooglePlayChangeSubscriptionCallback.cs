namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Interface for a class that handles callbacks for changing a subscription from the Google Play Store.
    /// </summary>
    interface IGooglePlayChangeSubscriptionCallback : IStorePurchaseCallback
    {
        /// <summary>
        /// Notify of a deferred subscription change with associated details.
        /// </summary>
        /// <param name="storeSpecificId"> The subscription product for which the purchase is deferred. </param>
        void OnSubscriptionChangeDeferredUntilRenewal(string storeSpecificId);

        /// <summary>
        /// Notify of a subscription change with associated details.
        /// </summary>
        /// <param name="storeSpecificId"> The new subscription product. </param>
        void OnSubscriptionChange(string storeSpecificId);
    }
}

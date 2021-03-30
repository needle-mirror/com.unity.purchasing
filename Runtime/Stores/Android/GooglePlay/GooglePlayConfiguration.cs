namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Access Google Play store specific configurations.
    /// </summary>
    public class GooglePlayConfiguration: IGooglePlayConfiguration
    {
        /// <summary>
        /// SetPublicKey is deprecated, nothing will be returns and no code will be executed.
        /// </summary>
        /// <param name="key">deprecated, nothing will be returns and no code will be executed.</param>
        public void SetPublicKey(string key) { }

        /// <summary>
        /// aggressivelyRecoverLostPurchases is deprecated, nothing will be returns and no code will be executed.
        /// </summary>
        public bool aggressivelyRecoverLostPurchases { get; set; }

        /// <summary>
        /// UsePurchaseTokenForTransactionId is deprecated, nothing will be returns and no code will be executed.
        /// </summary>
        /// <param name="usePurchaseToken">deprecated, nothing will be returns and no code will be executed.</param>
        public void UsePurchaseTokenForTransactionId(bool usePurchaseToken) { }
    }
}

using System;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Access Google Play store specific configurations.
    /// </summary>
    public interface IGooglePlayConfiguration: IStoreConfiguration
    {
        /// <summary>
        /// SetPublicKey is deprecated, nothing will be returns and no code will be executed.
        /// </summary>
        /// <param name="key">deprecated, nothing will be returns and no code will be executed.</param>
        [Obsolete("SetPublicKey is deprecated, nothing will be returns and no code will be executed. Will be removed soon.")]
        void SetPublicKey(string key);

        /// <summary>
        /// aggressivelyRecoverLostPurchases is deprecated, nothing will be returns and no code will be executed.
        /// </summary>
        [Obsolete("aggressivelyRecoverLostPurchases is deprecated, nothing will be returns and no code will be executed. Will be removed soon.")]
        bool aggressivelyRecoverLostPurchases { get; set; }

        /// <summary>
        /// UsePurchaseTokenForTransactionId is deprecated, nothing will be returns and no code will be executed.
        /// </summary>
        /// <param name="usePurchaseToken">deprecated, nothing will be returns and no code will be executed.</param>
        [Obsolete("UsePurchaseTokenForTransactionId is deprecated, nothing will be returns and no code will be executed. Will be removed soon.")]
        void UsePurchaseTokenForTransactionId(bool usePurchaseToken);
    }
}

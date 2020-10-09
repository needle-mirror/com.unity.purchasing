using System;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    public interface IGooglePlayConfiguration: IStoreConfiguration
    {
        [Obsolete("SetPublicKey is deprecated, nothing will be returns and no code will be executed. Will be removed soon.")]
        void SetPublicKey(string key);

        [Obsolete("aggressivelyRecoverLostPurchases is deprecated, nothing will be returns and no code will be executed. Will be removed soon.")]
        bool aggressivelyRecoverLostPurchases { get; set; }

        [Obsolete("UsePurchaseTokenForTransactionId is deprecated, nothing will be returns and no code will be executed. Will be removed soon.")]
        void UsePurchaseTokenForTransactionId(bool usePurchaseToken);
    }
}

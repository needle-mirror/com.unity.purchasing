using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing
{
    interface IGooglePlayStore : IStore
    {
        void SetChangeSubscriptionCallback(IGooglePlayChangeSubscriptionCallback changeSubscriptionCallback);
        void ChangeSubscription(ProductDefinition product, Product oldProduct, GooglePlayReplacementMode? desiredReplacementMode);
        void OnPause(bool isPaused);
        public IGooglePurchase GetGooglePurchase(string purchaseToken);
    }
}

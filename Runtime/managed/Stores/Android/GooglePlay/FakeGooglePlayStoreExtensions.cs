using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    public class FakeGooglePlayStoreExtensions: IGooglePlayStoreExtensions
    {
        public void UpgradeDowngradeSubscription(string oldSku, string newSku) { }

        public void UpgradeDowngradeSubscription(string oldSku, string newSku, int desiredProrationMode) { }

        public void RestoreTransactions(Action<bool> callback) { }

        public void FinishAdditionalTransaction(string productId, string transactionId) { }

        public void ConfirmSubscriptionPriceChange(string productId, Action<bool> callback) { }

        public void SetDeferredPurchaseListener(Action<Product> action) { }

        public void SetObfuscatedAccountId(string accountId) { }

        public void SetObfuscatedProfileId(string profileId) { }

        public void EndConnection() { }

        public Dictionary<string, string> GetProductJSONDictionary()
        {
            return null;
        }

        public void SetLogLevel(int level) { }

        public bool IsOwned(Product p)
        {
            return false;
        }
    }
}

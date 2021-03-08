namespace UnityEngine.Purchasing
{
    public class FakeGooglePlayStoreConfiguration: IGooglePlayConfiguration
    {
        public void SetPublicKey(string key) { }
        public bool aggressivelyRecoverLostPurchases { get; set; }
        public void UsePurchaseTokenForTransactionId(bool usePurchaseToken) { }
    }
}

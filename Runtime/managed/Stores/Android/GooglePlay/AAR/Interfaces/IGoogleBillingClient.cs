namespace UnityEngine.Purchasing.Interfaces
{
    interface IGoogleBillingClient
    {
        void StartConnection(IBillingClientStateListener billingClientStateListener);
        void EndConnection();
        AndroidJavaObject QueryPurchase(string skuType);
        void QuerySkuDetailsAsync(AndroidJavaObject skuDetailsParamsBuilder, SkuDetailsResponseListener listener);
        AndroidJavaObject LaunchBillingFlow(AndroidJavaObject sku, string oldSku, string oldPurchaseToken, int prorationMode);
        void ConsumeAsync(AndroidJavaObject consumeParams, GoogleConsumeResponseListener listener);
        void AcknowledgePurchase(AndroidJavaObject acknowledgePurchaseParams, GoogleAcknowledgePurchaseListener listener);
        void SetObfuscationAccountId(string obfuscationAccountId);
        void SetObfuscationProfileId(string obfuscationProfileId);
        void LaunchPriceChangeConfirmationFlow(AndroidJavaObject skuDetails, GooglePriceChangeConfirmationListener listener);
    }
}

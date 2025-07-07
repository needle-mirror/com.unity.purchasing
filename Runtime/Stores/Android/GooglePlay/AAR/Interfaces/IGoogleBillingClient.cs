using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Models;
using UnityEngine.Purchasing.Utils;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGoogleBillingClient
    {
        void StartConnection(IBillingClientStateListener billingClientStateListener);
        void EndConnection();
        bool IsReady();
        GoogleBillingConnectionState GetConnectionState();
        void QueryPurchasesAsync(string skuType, Action<IGoogleBillingResult, IEnumerable<AndroidJavaObject>> onQueryPurchasesResponse);
        void QueryProductDetailsAsync(List<string> skus, string type, Action<IGoogleBillingResult, List<AndroidJavaObject>> onProductDetailsResponseAction);
        AndroidJavaObject LaunchBillingFlow(AndroidJavaObject productDetails, string oldPurchaseToken, GooglePlayReplacementMode? replacementMode);
        void ConsumeAsync(string purchaseToken, Action<IGoogleBillingResult> onConsume);
        void AcknowledgePurchase(string purchaseToken, Action<IGoogleBillingResult> onAcknowledge);
        void SetObfuscationAccountId(string obfuscationAccountId);
        void SetObfuscationProfileId(string obfuscationProfileId);
    }
}

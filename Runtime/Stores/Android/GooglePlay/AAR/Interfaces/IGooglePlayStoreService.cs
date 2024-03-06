using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePlayStoreService
    {
        void RetrieveProducts(ReadOnlyCollection<ProductDefinition> products, Action<List<ProductDescription>> onProductsReceived, Action<GoogleRetrieveProductsFailureReason, GoogleBillingResponseCode> onRetrieveProductFailed);
        void Purchase(ProductDefinition product);
        void Purchase(ProductDefinition product, Product oldProduct, GooglePlayProrationMode? desiredProrationMode);
        void FinishTransaction(ProductDefinition product, string purchaseToken, Action<IGoogleBillingResult, IGooglePurchase> onTransactionFinished);
        void FetchPurchases(Action<List<IGooglePurchase>> onQueryPurchaseSucceed);
        void SetMaxConnectionAttempts(int maxConnectionAttempts);
        void SetObfuscatedAccountId(string obfuscatedAccountId);
        void SetObfuscatedProfileId(string obfuscatedProfileId);
        void ConfirmSubscriptionPriceChange(ProductDefinition product, Action<IGoogleBillingResult> onPriceChangeAction);
        void ResumeConnection();
        bool IsConnectionReady();
    }
}

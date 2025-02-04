#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePlayStoreService
    {
        void RetrieveProducts(IReadOnlyCollection<ProductDefinition> products,
            Action<List<ProductDescription>> onProductsReceived,
            Action<GoogleRetrieveProductException> onRetrieveProductsFailed);

        void Purchase(ProductDefinition product);
        void Purchase(ProductDefinition product, Product oldProduct, GooglePlayReplacementMode? desiredReplacementMode);
        void FinishTransaction(ProductDefinition? product, string? purchaseToken, Action<IGoogleBillingResult, IGooglePurchase> onTransactionFinished);
        void FetchPurchases(Action<List<IGooglePurchase>> onQueryPurchaseSucceed);
        void CheckEntitlement(ProductDefinition product, Action<ProductDefinition, EntitlementStatus> onEntitlementChecked);
        void SetObfuscatedAccountId(string obfuscatedAccountId);
        void SetObfuscatedProfileId(string obfuscatedProfileId);
        bool IsConnectionReady();
    }
}

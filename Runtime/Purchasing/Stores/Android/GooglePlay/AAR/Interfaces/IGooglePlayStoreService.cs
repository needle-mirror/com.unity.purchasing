#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePlayStoreService
    {
        void FetchProducts(IReadOnlyCollection<ProductDefinition> products,
            Action<List<ProductDescription>> onProductsReceived,
            Action<GoogleFetchProductException> onFetchProductsFailed);

        void Purchase(ProductDefinition product);
        void Purchase(ProductDefinition product, Order? currentOrder, GooglePlayReplacementMode? desiredReplacementMode);
        Task FinishTransaction(ProductDefinition? product, string? purchaseToken, Action<IGoogleBillingResult, IGooglePurchase> onTransactionFinished);
        void FetchPurchases(Action<List<IGooglePurchase>> onQueryPurchaseSucceed, Action<string?> onQueryPurchaseFailed);
        void CheckEntitlement(ProductDefinition product, Action<ProductDefinition, EntitlementStatus> onEntitlementChecked);
        void SetObfuscatedAccountId(string obfuscatedAccountId);
        void SetObfuscatedProfileId(string obfuscatedProfileId);
        bool IsConnectionReady();
    }
}

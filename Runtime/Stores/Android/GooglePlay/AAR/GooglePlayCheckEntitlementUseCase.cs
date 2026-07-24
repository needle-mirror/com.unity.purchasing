using System;
using System.Linq;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GooglePlayCheckEntitlementUseCase : IGooglePlayCheckEntitlementUseCase
    {
        readonly IGoogleQueryPurchasesUseCase m_GoogleQueryPurchasesUseCase;

        [Preserve]
        internal GooglePlayCheckEntitlementUseCase(IGoogleQueryPurchasesUseCase googleQueryPurchasesUseCase)
        {
            m_GoogleQueryPurchasesUseCase = googleQueryPurchasesUseCase;
        }

        public async void CheckEntitlement(ProductDefinition product, Action<ProductDefinition, EntitlementStatus> onEntitlementChecked)
        {
            if (product != null)
            {
                EntitlementStatus status;
                try
                {
                    var purchases = await m_GoogleQueryPurchasesUseCase.QueryPurchases();

                    var purchase = purchases.FirstOrDefault(PurchaseToCheckForEntitlement(product));
                    status = DetermineEntitlementStatus(purchase, product.type);
                }
                catch (Exception ex)
                {
                    // A failed purchases query doesn't mean the user is not entitled.
                    Debug.unityLogger.LogIAPWarning($"CheckEntitlement failed for {product.storeSpecificId}: {ex.Message}");
                    status = EntitlementStatus.Unknown;
                }

                onEntitlementChecked?.Invoke(product, status);
            }
            else
            {
                onEntitlementChecked?.Invoke(product, EntitlementStatus.Unknown);
            }
        }

        static Func<IGooglePurchase, bool> PurchaseToCheckForEntitlement(ProductDefinition product)
        {
            return purchase => purchase != null
                && purchase.sku == product.storeSpecificId
                && purchase.IsPurchased();
        }

        static EntitlementStatus DetermineEntitlementStatus(IGooglePurchase purchase, ProductType type)
        {
            var status = purchase == null
                ? EntitlementStatus.NotEntitled
                : type == ProductType.Consumable
                    ? EntitlementStatus.EntitledUntilConsumed
                    : purchase.IsAcknowledged() ? EntitlementStatus.FullyEntitled : EntitlementStatus.EntitledButNotFinished;
            return status;
        }
    }
}

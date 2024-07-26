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
                var purchases = await m_GoogleQueryPurchasesUseCase.QueryPurchases();

                var purchase = purchases.FirstOrDefault(PurchaseToCheckForEntitlement(product));
                var status = DetermineEntitlementStatus(purchase, product.type);

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

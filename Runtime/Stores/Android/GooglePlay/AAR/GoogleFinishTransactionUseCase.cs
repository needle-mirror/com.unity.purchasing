#nullable enable

using System;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class GoogleFinishTransactionUseCase : IGoogleFinishTransactionUseCase
    {
        readonly IGoogleBillingClient m_BillingClient;
        readonly IGoogleQueryPurchasesUseCase m_GoogleQueryPurchasesUseCase;

        [Preserve]
        internal GoogleFinishTransactionUseCase(IGoogleBillingClient billingClient,
            IGoogleQueryPurchasesUseCase googleQueryPurchasesUseCase)
        {
            m_BillingClient = billingClient;
            m_GoogleQueryPurchasesUseCase = googleQueryPurchasesUseCase;
        }

        public async Task FinishTransaction(ProductDefinition? product, string? purchaseToken,
            Action<IGoogleBillingResult, IGooglePurchase> onTransactionFinished)
        {
            var purchase = await m_GoogleQueryPurchasesUseCase.GetPurchaseByToken(purchaseToken);
            if (purchase != null && purchase.IsPurchased())
            {
                FinishTransactionForPurchase(purchase, product, purchaseToken, onTransactionFinished);
            }
            else
            {
                throw new Exception($"Purchase not found for Product: {product?.storeSpecificId}, Purchase Token: {purchaseToken}");
            }
        }

        void FinishTransactionForPurchase(IGooglePurchase purchase, ProductDefinition? product,
            string? purchaseToken,
            Action<IGoogleBillingResult, IGooglePurchase> onTransactionFinished)
        {
            if (product != null && product.type == ProductType.Consumable)
            {
                m_BillingClient.ConsumeAsync(purchaseToken, result => onTransactionFinished(result, purchase));
            }
            else if (!purchase.IsAcknowledged())
            {
                m_BillingClient.AcknowledgePurchase(purchaseToken, result => onTransactionFinished(result, purchase));
            }
        }
    }
}

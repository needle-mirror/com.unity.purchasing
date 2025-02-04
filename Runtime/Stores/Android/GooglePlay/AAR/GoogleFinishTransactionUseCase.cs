#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    public class GoogleFinishTransactionUseCase : IGoogleFinishTransactionUseCase
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

        public async void FinishTransaction(ProductDefinition? product, string? purchaseToken,
            Action<IGoogleBillingResult, IGooglePurchase> onTransactionFinished)
        {
            try
            {
                var purchase = await m_GoogleQueryPurchasesUseCase.GetPurchaseByToken(purchaseToken);
                if (purchase != null && purchase.IsPurchased())
                {
                    FinishTransactionForPurchase(purchase, product, purchaseToken, onTransactionFinished);
                }
            }
            catch (InvalidOperationException e)
            {
                Debug.unityLogger.LogIAPError($"FinishTransaction exception: {e}");
            }
        }

        private void FinishTransactionForPurchase(IGooglePurchase purchase, ProductDefinition? product,
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

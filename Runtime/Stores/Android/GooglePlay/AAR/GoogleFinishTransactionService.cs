#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing
{
    class GoogleFinishTransactionService : IGoogleFinishTransactionService
    {
        readonly IGoogleBillingClient m_BillingClient;
        readonly IGoogleQueryPurchasesService m_GoogleQueryPurchasesService;

        internal GoogleFinishTransactionService(IGoogleBillingClient billingClient,
            IGoogleQueryPurchasesService googleQueryPurchasesService)
        {
            m_BillingClient = billingClient;
            m_GoogleQueryPurchasesService = googleQueryPurchasesService;
        }

        public async void FinishTransaction(ProductDefinition product, string purchaseToken,
            Action<IGoogleBillingResult, IGooglePurchase> onTransactionFinished)
        {
            try
            {
                var purchase = await FindPurchase(purchaseToken);
                if (purchase.IsPurchased())
                {
                    FinishTransactionForPurchase(purchase, product, purchaseToken, onTransactionFinished);
                }
            }
            catch (InvalidOperationException) { }
        }

        async Task<IGooglePurchase> FindPurchase(string purchaseToken)
        {
            var purchases = await m_GoogleQueryPurchasesService.QueryPurchases();
            var purchaseToFinish =
                purchases.NonNull().First(purchase => purchase.purchaseToken == purchaseToken);

            return purchaseToFinish;
        }

        private void FinishTransactionForPurchase(IGooglePurchase purchase, ProductDefinition product,
            string purchaseToken,
            Action<IGoogleBillingResult, IGooglePurchase> onTransactionFinished)
        {
            if (product.type == ProductType.Consumable)
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

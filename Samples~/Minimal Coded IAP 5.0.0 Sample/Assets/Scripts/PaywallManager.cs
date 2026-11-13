using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;

namespace Samples.Purchasing.IAP5.Minimal
{
    public class PaywallManager : MonoBehaviour
    {
        public Text inAppConsole;

        PurchasingController m_PurchasingController;
        string m_StoreName;

        protected void Awake()
        {
            InitializeIAP();
        }

        async void InitializeIAP()
        {
            m_StoreName = UnityIAPServices.GetDefaultStore();
            m_PurchasingController = new PurchasingController(m_StoreName);

            m_PurchasingController.OnPurchasePending += OnPurchasePending;
            m_PurchasingController.OnPurchaseConfirmed += OnPurchaseConfirmed;

            await m_PurchasingController.Connect();

            FetchProducts();
        }

        void OnPurchasePending(string storeName, PendingOrder order)
        {
            // Add your validations here before confirming the purchase.

            // Before confirming the purchase, reward the entitlement to the player.
            m_PurchasingController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(string storeName, Order order)
        {
            switch (order)
            {
                case FailedOrder failedOrder:
                    LogConsole($"Purchase confirmation failed: {failedOrder.CartOrdered.Items().First().Product.definition.id}, {failedOrder.FailureReason.ToString()}, {failedOrder.Details}");
                    break;
                case ConfirmedOrder:
                    LogConsole($"Purchase completed:  {order.CartOrdered.Items().First().Product.definition.id}");
                    break;
            }
        }

        void FetchProducts()
        {
            var initialProductsToFetch = new List<ProductDefinition>
            {
                new("com.unity.iap.test.30.gems", ProductType.Consumable),
                new("com.unity.iap.test.no.ads", ProductType.NonConsumable),
                new("com.unity.iap.test.adventure.pass", ProductType.Subscription)
            };

            m_PurchasingController.FetchProducts(initialProductsToFetch, m_StoreName);
        }

        public void InitiatePurchase(string productId)
        {
            var product = m_PurchasingController?.GetProducts(m_StoreName).FirstOrDefault(product => product.definition.id == productId);

            if (product != null)
            {
                m_PurchasingController?.PurchaseProduct(product);
            }
            else
            {
                LogConsole($"The product service has no product with the ID {productId}");
            }
        }

        public void RestorePurchases()
        {
            m_PurchasingController.RestoreTransactions(OnTransactionsRestored, m_StoreName);
        }

        void OnTransactionsRestored(string storeName, bool success, string error)
        {
            LogConsole("Transactions restored: " + success);
        }

        void LogConsole(string msg)
        {
            Debug.Log(msg);
            if (inAppConsole.text.Length > 0)
            {
                inAppConsole.text = "\n" + inAppConsole.text;
            }
            inAppConsole.text = msg + inAppConsole.text;
        }
    }
}

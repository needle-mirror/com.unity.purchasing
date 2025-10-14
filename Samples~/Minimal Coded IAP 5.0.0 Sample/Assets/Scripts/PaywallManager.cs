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

        StoreController m_StoreController;

        protected void Awake()
        {
            InitializeIAP();
        }

        async void InitializeIAP()
        {
            m_StoreController = UnityIAPServices.StoreController();

            m_StoreController.OnPurchasePending += OnPurchasePending;
            m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;

            await m_StoreController.Connect();

            FetchProducts();
        }

        void OnPurchasePending(PendingOrder order)
        {
            // Add your validations here before confirming the purchase.

            // Before confirming the purchase, reward the entitlement to the player.
            m_StoreController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(Order order)
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

            m_StoreController.FetchProducts(initialProductsToFetch);
        }

        public void InitiatePurchase(string productId)
        {
            var product = m_StoreController?.GetProducts().FirstOrDefault(product => product.definition.id == productId);

            if (product != null)
            {
                m_StoreController?.PurchaseProduct(product);
            }
            else
            {
                LogConsole($"The product service has no product with the ID {productId}");
            }
        }

        public void RestorePurchases()
        {
            m_StoreController.RestoreTransactions(OnTransactionsRestored);
        }

        void OnTransactionsRestored(bool success, string error)
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

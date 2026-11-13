using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;

namespace Samples.Purchasing.AppleAppStore.GettingIntroductoryPrices
{
    public class GettingIntroductoryPrices : MonoBehaviour
    {
        PurchasingController m_PurchasingController;
        string m_StoreName;

        public string subscriptionProductId = "com.mycompany.mygame.my_vip_pass_subscription";
        public ProductType subscriptionType = ProductType.Subscription;

        public Text isSubscribedText;
        public bool isSubscribed;

        void Awake()
        {
            InitializeIAP();
        }

        async void InitializeIAP()
        {
            m_StoreName = UnityIAPServices.GetDefaultStore();
            m_PurchasingController = new PurchasingController(m_StoreName);

            m_PurchasingController.OnPurchasePending += OnPurchasePending;
            m_PurchasingController.OnPurchaseConfirmed += OnPurchaseConfirmed;
            m_PurchasingController.OnPurchaseFailed += OnPurchaseFailed;
            m_PurchasingController.OnCheckEntitlement += OnCheckEntitlement;
            m_PurchasingController.OnProductsFetched += OnProductsFetched;

            await m_PurchasingController.Connect();

            FetchProducts();
        }

        void FetchProducts()
        {
            var initialProductsToFetch = new List<ProductDefinition>
            {
                new(subscriptionProductId, subscriptionType)
            };

            m_PurchasingController.FetchProducts(initialProductsToFetch, m_StoreName);
        }

        void OnPurchasePending(string storeName, PendingOrder order)
        {
            m_PurchasingController.ConfirmPurchase(order);
        }

        void OnPurchaseConfirmed(string storeName, Order order)
        {
            if (order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id == subscriptionProductId)
            {
                Debug.Log($"Purchase confirmed for product: {subscriptionProductId}");
                isSubscribed = true;
            }
        }

        public void OnPurchaseFailed(string storeName, FailedOrder order)
        {
            Debug.Log($"Purchase failed - Product: '{order.CartOrdered.Items().FirstOrDefault()?.Product.definition.id}'," +
                $" Purchase failure reason: {order.FailureReason}," +
                $" Purchase failure details: {order.Details}");
        }

        void OnCheckEntitlement(string storeName, Entitlement entitlement)
        {
            if (entitlement.Product.definition.id == subscriptionProductId)
            {
                switch (entitlement.Status)
                {
                    case EntitlementStatus.FullyEntitled:
                        isSubscribed = true;
                        UpdateUI();
                        break;
                    default:
                        isSubscribed = false;
                        UpdateUI();
                        break;
                }
            }
        }

        void OnProductsFetched(string storeName, List<Product> obj)
        {
            CheckSubscription();
        }

        public void BuySubscription()
        {
            m_PurchasingController.PurchaseProduct(subscriptionProductId, m_StoreName);
        }

        void CheckSubscription()
        {
            var product = m_PurchasingController.GetProducts(m_StoreName).FirstOrDefault(p => p.definition.id == subscriptionProductId);
            if (product != null)
            {
                m_PurchasingController.CheckEntitlement(product);
            }
        }

        void UpdateUI()
        {
            isSubscribedText.text = isSubscribed ? "You are subscribed" : "You are not subscribed";
            isSubscribedText.text += $"\nIntroductory Price Information for {subscriptionProductId}:\n{GetIntroductoryPriceForProductId(subscriptionProductId)}";
        }

        string GetIntroductoryPriceForProductId(string productId)
        {
            var introductoryPrices = m_PurchasingController.AppleStoreExtendedProductService?.GetIntroductoryPriceDictionary();
            var subscriptionIntroductionPriceInfo = introductoryPrices?[productId];

            return subscriptionIntroductionPriceInfo;
        }
    }
}
